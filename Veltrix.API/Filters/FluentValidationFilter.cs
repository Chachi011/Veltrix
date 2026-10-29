using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

        // A fresh dictionary, not context.ModelState: model binding already put entries like "File" there,
        // and ModelState keys ignore case, so our camelCase "file" would be merged under "File".
        var errors = new ModelStateDictionary();

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
                errors.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage);
            }
        }

        if (errors.ErrorCount > 0)
        {
            var factory = services.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext, errors, StatusCodes.Status400BadRequest, "Validation failed");

            // A JsonResult, not a BadRequestObjectResult: [Produces("application/json")] on the controllers rewrites the
            // content type of every ObjectResult, and this error must be "application/problem+json" like all the others
            context.Result = new JsonResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentType = "application/problem+json"
            };
            return;
        }

        await next();
    }
}
