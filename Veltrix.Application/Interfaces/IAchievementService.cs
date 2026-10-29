using Veltrix.Application.DTOs.Achievements;

namespace Veltrix.Application.Interfaces;

/// <summary>Achievements (spec 4.6). They are UNLOCKED by the round settlement (RoundSettlementService), not here.</summary>
public interface IAchievementService
{
    /// <summary>The whole catalog, with how many players unlocked each achievement (public).</summary>
    Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>The catalog for the logged-in player: unlocked ones with the date, the others with their progress.</summary>
    Task<IReadOnlyList<MyAchievementDto>> GetMineAsync(CancellationToken ct = default);
}
