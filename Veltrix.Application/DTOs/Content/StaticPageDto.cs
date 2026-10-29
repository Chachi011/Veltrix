namespace Veltrix.Application.DTOs.Content;

/// <summary>An editable content page (game rules, "why there is no winning strategy").</summary>
public sealed class StaticPageDto
{
    public string Slug { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown: paragraphs, numbered / bulleted lists, **bold** and *italic*.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
