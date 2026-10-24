using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Veltrix.Infrastructure.Storage;

namespace Veltrix.API.Extensions;

public static class AvatarFilesExtensions
{
    /// <summary>Avatars are stored in &lt;API folder&gt;/{Storage:AvatarsPath} (default "uploads/avatars").</summary>
    public static IServiceCollection AddAvatarStorage(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var relativePath = configuration["Storage:AvatarsPath"] ?? "uploads/avatars";

        services.Configure<AvatarStorageOptions>(options =>
        {
            options.RootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
            options.RequestPath = "/uploads/avatars";
        });

        return services;
    }

    /// <summary>
    /// Serves ONLY the avatars folder as static files (GET /uploads/avatars/{file}).
    /// Nothing else on disk is reachable.
    /// </summary>
    public static WebApplication UseAvatarFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<AvatarStorageOptions>>().Value;
        Directory.CreateDirectory(options.RootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(options.RootPath),
            RequestPath = options.RequestPath,
            // Stops browsers from "guessing" a different content type (e.g. treating a file as HTML/script)
            OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
        });

        return app;
    }
}
