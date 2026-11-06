namespace Veltrix.Application.Common.Files;

/// <summary>Avatar upload limits, shared by the request validator and the service.</summary>
public static class AvatarRules
{
    public const long MaxBytes = 2 * 1024 * 1024;

    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
}
