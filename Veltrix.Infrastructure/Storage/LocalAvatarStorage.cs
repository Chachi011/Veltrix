using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Interfaces;

namespace Veltrix.Infrastructure.Storage;

public sealed class AvatarStorageOptions
{
    /// <summary>Absolute folder on disk. Set by the API at startup.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>URL prefix the API serves the folder under.</summary>
    public string RequestPath { get; set; } = "/uploads/avatars";
}

/// <summary>Saves avatars as files on the API server's disk.</summary>
public sealed class LocalAvatarStorage : IAvatarStorage
{
    private readonly AvatarStorageOptions _options;
    private readonly ILogger<LocalAvatarStorage> _logger;

    public LocalAvatarStorage(IOptions<AvatarStorageOptions> options, ILogger<LocalAvatarStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_options.RootPath);

        // Random server-chosen name: the user's own file name is never used,
        // so names like "../../appsettings.json" cannot escape the folder or overwrite files
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_options.RootPath, fileName), content, ct);

        return $"{_options.RequestPath}/{fileName}";
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith(_options.RequestPath + "/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        // GetFileName strips any directory parts, so only files inside RootPath can be deleted
        var path = Path.Combine(_options.RootPath, Path.GetFileName(url));

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {
            // An old avatar left on disk is not worth failing the request for
            _logger.LogWarning(ex, "Could not delete old avatar {Path}", path);
        }

        return Task.CompletedTask;
    }
}
