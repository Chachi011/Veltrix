using Veltrix.Application.DTOs.Leaderboard;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Stats;

namespace Veltrix.Application.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardDto> GetAsync(LeaderboardQuery query, CancellationToken ct = default);
}

public interface IStatsService
{
    Task<MyStatsDto> GetMyStatsAsync(MyStatsQuery query, CancellationToken ct = default);
}

/// <summary>Responsible play tools: daily bet limit, session reminder, self-exclusion.</summary>
public interface ILimitsService
{
    Task<PlayerLimitsDto> GetMyLimitsAsync(CancellationToken ct = default);
    Task<PlayerLimitsDto> UpdateMyLimitsAsync(UpdateLimitsRequest request, CancellationToken ct = default);
    Task<PlayerLimitsDto> StartSelfExclusionAsync(SelfExclusionRequest request, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<NotificationPageDto> GetMyAsync(NotificationQuery query, CancellationToken ct = default);
    Task MarkReadAsync(int id, CancellationToken ct = default);

    /// <summary>Returns how many notifications were marked.</summary>
    Task<int> MarkAllReadAsync(CancellationToken ct = default);
}
