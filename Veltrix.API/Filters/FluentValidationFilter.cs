using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Veltrix.API.Filters;

/// <summary>
/// Runs before every controller action: if a FluentValidation validator exists for an action
/// argument, the argument is validated. Invalid input never reaches the service layer and the
/// client gets 400 with errors grouped by field (ValidationProblemDetails).
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                // "Email" -> "email", so field names match the JSON the Angular client sends
                context.ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var factory = services.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest, "Validation failed");

            context.Result = new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
            return;
        }

        await next();
    }
}
