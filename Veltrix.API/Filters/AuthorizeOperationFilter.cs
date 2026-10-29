using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Veltrix.API.Filters;

/// <summary>
/// Adds the Swagger lock icon (Bearer security requirement) ONLY to endpoints that really need a JWT,
/// i.e. that have [Authorize] (on the action or controller) and no [AllowAnonymous].
/// A global requirement would put the lock on login/register too, which is misleading.
/// </summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public const string SchemeId = "Bearer";

    private static readonly OpenApiSecurityScheme BearerScheme = new()
    {
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeId }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Endpoint metadata contains the attributes of both the controller and the action
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // Same rule ASP.NET Core itself uses: [AllowAnonymous] anywhere wins over [Authorize]
        if (metadata.OfType<IAllowAnonymous>().Any())
            return;

        var authorizeAttributes = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeAttributes.Count == 0)
            return;

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new() { [BearerScheme] = Array.Empty<string>() }
        };

        // Document the auth errors even if the action did not declare them
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Missing or invalid access token" });

        var roles = authorizeAttributes
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        if (roles.Count > 0)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Requires role: " + string.Join(" or ", roles) });

            // Show the required roles in the endpoint description as well
            var note = $"**Roles:** {string.Join(", ", roles)}";
            operation.Description = string.IsNullOrEmpty(operation.Description) ? note : $"{operation.Description}\n\n{note}";
        }
    }
}
