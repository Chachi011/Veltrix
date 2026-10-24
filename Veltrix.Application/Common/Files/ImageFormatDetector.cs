namespace Veltrix.Application.Common.Files;

/// <summary>
/// Detects the REAL image type from the first bytes of the file ("magic bytes").
/// The file name and Content-Type header are chosen by the client and can lie;
/// the bytes cannot (e.g. an .exe renamed to photo.png is rejected here).
/// </summary>
public static class ImageFormatDetector
{
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <returns>The file extension to save with (".jpg", ".png", ".webp"), or null if not a supported image.</returns>
    public static string? DetectExtension(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(Jpeg))
            return ".jpg";

        if (content.StartsWith(Png))
            return ".png";

        // WEBP layout: "RIFF" + 4-byte size + "WEBP"
        if (content.Length >= 12 && content.StartsWith(Riff) && content.Slice(8, 4).SequenceEqual(Webp))
            return ".webp";

        return null;
    }
}
