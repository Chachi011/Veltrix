using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Domain.Common;

namespace Veltrix.API.Middleware;

/// <summary>
/// Catches every exception thrown anywhere in the request pipeline and turns it into
/// a ProblemDetails (RFC 7807) JSON response. Controllers therefore contain no try/catch.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client closed the connection; nothing to answer
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = exception switch
        {
            AppException app => Create(app.StatusCode, app.Title, app.Message),

            // Business rules from the Domain layer ("betting is closed", "insufficient balance")
            InvalidRoundTransitionException transition => Create(StatusCodes.Status409Conflict, "Conflict", transition.Message),
            SelfExcludedException excluded => Create(StatusCodes.Status403Forbidden, "Forbidden", excluded.Message),
            DomainException domain => Create(StatusCodes.Status400BadRequest, "Request rejected", domain.Message),

            // RowVersion mismatch: someone else changed the same row at the same moment
            DbUpdateConcurrencyException => Create(StatusCodes.Status409Conflict, "Conflict",
                "The data was changed by another request. Please try again."),

            // Usually a UNIQUE index or CHECK constraint violation
            DbUpdateException => Create(StatusCodes.Status409Conflict, "Conflict",
                "The operation conflicts with existing data."),

            // Thrown by the web server itself, e.g. 413 when an upload exceeds [RequestSizeLimit]
            BadHttpRequestException badRequest => Create(badRequest.StatusCode,
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Payload too large" : "Bad request",
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "The request body is too large." : badRequest.Message),

            _ => Create(StatusCodes.Status500InternalServerError, "Server error",
                _environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.")
        };

        if (problem.Status >= 500)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("{Status} {Title}: {Detail} ({Method} {Path})",
                problem.Status, problem.Title, problem.Detail, context.Request.Method, context.Request.Path);

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }

    private static ProblemDetails Create(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = $"https://httpstatuses.io/{status}"
    };
}
