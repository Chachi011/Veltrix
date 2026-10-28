using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>Per-user UI settings, so they follow the user to any device. 1-to-1 with User.</summary>
public class UserPreference
{
    /// <summary>Georgian is the default UI language (spec 4.7); English is the other one.</summary>
    public const string DefaultLanguage = "ka";

    public static readonly IReadOnlyList<string> Languages = new[] { "ka", "en" };

    public int UserId { get; set; }
    public ThemePreference Theme { get; set; } = ThemePreference.System;
    public bool SoundEnabled { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>"ka" or "en".</summary>
    public string Language { get; set; } = DefaultLanguage;

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
