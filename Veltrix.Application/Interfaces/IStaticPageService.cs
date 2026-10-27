using Veltrix.Application.DTOs.Content;

namespace Veltrix.Application.Interfaces;

public interface IStaticPageService
{
    /// <summary>The page in the requested language, or in English if that translation does not exist.</summary>
    Task<StaticPageDto> GetAsync(string slug, string? language, CancellationToken ct = default);
}
