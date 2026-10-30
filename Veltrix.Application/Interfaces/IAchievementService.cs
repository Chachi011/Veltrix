using Veltrix.Application.DTOs.Achievements;

namespace Veltrix.Application.Interfaces;

/// <summary>
/// Achievements (spec 4.6). Play ones are unlocked by the round settlement; Learn and Safety ones by their action
/// (verifying a round, reading the rules, saving a daily limit). This service reads them and records the rules being read.
/// </summary>
public interface IAchievementService
{
    /// <summary>The active catalog (safety and learning first), with how many players unlocked each achievement (public).</summary>
    Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// The catalog for the logged-in player: unlocked ones with the date, the others with their progress. A player who
    /// cannot bet right now (self-excluded, daily limit used up) gets no task that asks them to play.
    /// </summary>
    Task<MyAchievementsDto> GetMineAsync(CancellationToken ct = default);

    /// <summary>The logged-in player read a content page to its end; reading the rules completes "Know the rules". 404 for an unknown page.</summary>
    Task RecordPageReadAsync(string slug, CancellationToken ct = default);
}
