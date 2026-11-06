using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Assistant;
using Veltrix.Infrastructure.Auth;
using Veltrix.Infrastructure.Email;
using Veltrix.Infrastructure.Engine;
using Veltrix.Infrastructure.Jobs;
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
        services.AddScoped<IRoundService, RoundService>();
        services.AddScoped<IBetService, BetService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IFairnessService, FairnessService>();

        // Phase 4: player tools and admin panel
        services.AddMemoryCache(); // leaderboard cache
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<ILimitsService, LimitsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IAdminStatsService, AdminStatsService>();
        services.AddScoped<IStaticPageService, StaticPageService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<AchievementAwarder>();

        AddAssistant(services, configuration);

        services.AddScoped<DatabaseInitializer>();

        // Small periodic jobs, each in its own DI scope per run
        services.AddHostedService<DailyBonusReminderJob>();
        services.AddHostedService<DailyStatisticsJob>();

        AddCrashEngine(services, configuration);

        return services;
    }

    /// <summary>
    /// The AI assistant. Veltrix does not call an AI model itself: it posts the question to an n8n webhook, and the
    /// n8n workflow (docs/n8n/veltrix-assistant.json) talks to the model. The rules for the model live here, in
    /// AssistantPrompts, so they are versioned and tested with the code.
    /// </summary>
    private static void AddAssistant(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AssistantOptions>().Bind(configuration.GetSection(AssistantOptions.SectionName));

        // ONE HttpClient for the whole app (a new HttpClient per request would run out of sockets).
        // PooledConnectionLifetime makes it pick up DNS changes; the timeout per call comes from AssistantOptions.
        services.AddSingleton<IAssistantClient>(sp => new N8nAssistantClient(
            new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
            {
                Timeout = Timeout.InfiniteTimeSpan
            },
            sp.GetRequiredService<IOptions<AssistantOptions>>(),
            sp.GetRequiredService<ILogger<N8nAssistantClient>>()));

        services.AddScoped<IAssistantService, AssistantService>();
    }

    private static void AddCrashEngine(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CrashEngineOptions>().Bind(configuration.GetSection(CrashEngineOptions.SectionName));
        services.AddScoped<RoundSettlementService>();

        // ONE engine instance, reachable both as the hosted background loop and as IRoundEngineControl
        services.AddSingleton<CrashGameEngine>();
        services.AddSingleton<IRoundEngineControl>(sp => sp.GetRequiredService<CrashGameEngine>());

        var enabled = configuration.GetValue(CrashEngineOptions.SectionName + ":" + nameof(CrashEngineOptions.Enabled), true);
        if (enabled)
            services.AddHostedService(sp => sp.GetRequiredService<CrashGameEngine>());
    }
}
