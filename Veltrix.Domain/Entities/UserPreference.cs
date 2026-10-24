using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>Per-user UI settings, so they follow the user to any device. 1-to-1 with User.</summary>
public class UserPreference
{
    public const string DefaultLanguage = "en";

    public int UserId { get; set; }
    public ThemePreference Theme { get; set; } = ThemePreference.System;
    public bool SoundEnabled { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>"en" or "ka".</summary>
    public string Language { get; set; } = DefaultLanguage;

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
