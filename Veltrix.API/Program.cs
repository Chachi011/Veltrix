using Serilog;
using Veltrix.API.Extensions;
using Veltrix.API.Hubs;
using Veltrix.API.Middleware;
using Veltrix.Application;
using Veltrix.Infrastructure;
using Veltrix.Infrastructure.Persistence;

// Bootstrap logger: catches errors that happen before the real Serilog config is loaded
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog settings (console + rolling file) come from the "Serilog" section of appsettings.json
    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Each layer registers its own services
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration)
        .AddAvatarStorage(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // Order matters. Request logging is outermost, so it logs the FINAL status code (e.g. 401), after the
    // exception middleware has turned an exception into a ProblemDetails response; the exception
    // middleware wraps everything else.
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "VELTRIX API v1");
            options.DocumentTitle = "VELTRIX API";
        });
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseAvatarFiles();
    app.UseCors(ApiServiceExtensions.AngularCorsPolicy);
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHub<CrashHub>(ApiServiceExtensions.HubsPath + "/crash");

    // Apply migrations + seed test users (controlled by the "Database" config section)
    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown on purpose by "dotnet ef" tools; it is not a real crash
    Log.Fatal(ex, "VELTRIX API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Makes the generated Program class public, so the integration tests (WebApplicationFactory<Program>)
// can start this exact app in memory
public partial class Program { }
