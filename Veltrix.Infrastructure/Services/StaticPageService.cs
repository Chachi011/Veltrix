using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.DTOs.Content;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Content pages stored in the StaticPages table, so the text can change without a new deployment.</summary>
public sealed class StaticPageService : IStaticPageService
{
    private readonly ApplicationDbContext _db;

    public StaticPageService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Every page exists in English (the seed). Deliberately NOT the UI default language (Georgian): the Georgian
    /// text of a page may live elsewhere (the client's ka.json) or not exist yet.
    /// </summary>
    private const string FallbackLanguage = "en";

    public async Task<StaticPageDto> GetAsync(string slug, string? language, CancellationToken ct = default)
    {
        var wanted = string.IsNullOrWhiteSpace(language) ? FallbackLanguage : language.Trim().ToLowerInvariant();

        // The requested language first, English as the fallback (a page may not be translated yet)
        var pages = await _db.StaticPages
            .AsNoTracking()
            .Where(p => p.Slug == slug && (p.Language == wanted || p.Language == FallbackLanguage))
            .ToListAsync(ct);

        var page = pages.FirstOrDefault(p => p.Language == wanted) ?? pages.FirstOrDefault()
            ?? throw new NotFoundException($"Page '{slug}' was not found.");

        return new StaticPageDto
        {
            Slug = page.Slug,
            Language = page.Language,
            Title = page.Title,
            Content = page.Content,
            UpdatedAt = page.UpdatedAt
        };
    }
}
