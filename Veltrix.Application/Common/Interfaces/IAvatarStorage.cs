namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// Where avatar files are kept. The MVP saves them on the API server's disk;
/// it could be swapped for cloud storage without touching ProfileService.
/// </summary>
public interface IAvatarStorage
{
    /// <returns>The public URL of the saved file.</returns>
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default);

    /// <summary>Deletes a file previously returned by SaveAsync. Unknown URLs are ignored.</summary>
    Task DeleteAsync(string url, CancellationToken ct = default);
}
