using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Veltrix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> in this assembly as IValidator<T>
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
