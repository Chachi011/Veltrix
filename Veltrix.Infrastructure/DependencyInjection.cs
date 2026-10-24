using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Auth;
using Veltrix.Infrastructure.Email;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;
using Veltrix.Infrastructure.Storage;

namespace Veltrix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Fails at startup (not at first login) if the JWT config is missing or too weak
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));

        // TimeProvider instead of DateTime.UtcNow, so tests can control "now"
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, LogEmailSender>();
        services.AddSingleton<IAvatarStorage, LocalAvatarStorage>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
