namespace Veltrix.Domain.Entities;

/// <summary>Editable content pages (game rules, "why there is no winning strategy"), per language.</summary>
public class StaticPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Language { get; set; } = UserPreference.DefaultLanguage;
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
