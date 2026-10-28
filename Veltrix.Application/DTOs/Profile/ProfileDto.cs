using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Profile;

/// <summary>The logged-in user's OWN profile (contains private data).</summary>
public sealed class ProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Balance { get; set; }
}

public sealed class UpdateProfileRequest
{
    public string Nickname { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// What ANYONE (even a guest) may see about a player. A separate class from ProfileDto on purpose:
/// private fields (email, role, balance, date of birth) do not exist here, so they can never leak by mistake.
/// </summary>
public sealed class PublicProfileDto
{
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime MemberSince { get; set; }
    public int Level { get; set; }
    public string LevelTitle { get; set; } = string.Empty;
    public int TotalBets { get; set; }
    public int CashOutCount { get; set; }
    public decimal BiggestMultiplier { get; set; }
    public decimal BiggestWin { get; set; }
    public IReadOnlyList<BestCashOutDto> BestCashOuts { get; set; } = Array.Empty<BestCashOutDto>();
}

/// <summary>UI settings stored per account, so they follow the user to any device.</summary>
public sealed class UserPreferencesDto
{
    /// <summary>System (follow the device), Dark or Light.</summary>
    public ThemePreference Theme { get; set; }

    public bool SoundEnabled { get; set; }

    /// <summary>False = reduce motion (no counting numbers, no chart animations, no shake).</summary>
    public bool AnimationsEnabled { get; set; }

    /// <summary>UI language: "ka" (Georgian, the default) or "en".</summary>
    public string Language { get; set; } = UserPreference.DefaultLanguage;

    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdatePreferencesRequest
{
    public ThemePreference Theme { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>"ka" or "en".</summary>
    public string Language { get; set; } = UserPreference.DefaultLanguage;
}

public sealed class BestCashOutDto
{
    public decimal Multiplier { get; set; }
    public decimal Payout { get; set; }
    public DateTime CashedOutAt { get; set; }
}
