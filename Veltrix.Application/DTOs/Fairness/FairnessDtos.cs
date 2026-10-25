using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Fairness;

public sealed class FairnessRoundDto
{
    public int RoundId { get; set; }
    public long RoundNumber { get; set; }
    public RoundState State { get; set; }
    public string ServerSeedHash { get; set; } = string.Empty;

    /// <summary>Null while the round is not over.</summary>
    public string? ServerSeed { get; set; }

    /// <summary>Null while the round is not over.</summary>
    public decimal? CrashPoint { get; set; }

    /// <summary>True when seed, hash and crash point were checked by the server and all match.</summary>
    public bool? IsVerified { get; set; }

    public string Algorithm { get; set; } = string.Empty;
}

/// <summary>Anyone can paste a revealed seed (and optionally the published hash / crash point) to check it.</summary>
public sealed class VerifyFairnessRequest
{
    public string ServerSeed { get; set; } = string.Empty;
    public string? ServerSeedHash { get; set; }
    public decimal? CrashPoint { get; set; }
}

public sealed class VerifyFairnessResult
{
    public string ComputedHash { get; set; } = string.Empty;
    public decimal ComputedCrashPoint { get; set; }

    /// <summary>Null when no hash was given.</summary>
    public bool? HashMatches { get; set; }

    /// <summary>Null when no crash point was given.</summary>
    public bool? CrashPointMatches { get; set; }

    public string Algorithm { get; set; } = string.Empty;
}
