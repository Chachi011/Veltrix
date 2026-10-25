using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Realtime;

namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// Pushes real-time events to browsers. Implemented with SignalR in the API layer;
/// the engine and services only know this interface, not SignalR itself.
/// </summary>
public interface IGameNotifier
{
    Task RoundStateChangedAsync(string gameSlug, RoundStateChangedEvent payload);
    Task MultiplierTickAsync(string gameSlug, MultiplierTickEvent payload);
    Task RoundCrashedAsync(string gameSlug, RoundCrashedEvent payload);
    Task BetPlacedAsync(string gameSlug, BetPlacedEvent payload);
    Task PlayerCashedOutAsync(string gameSlug, PlayerCashedOutEvent payload);
    Task BalanceUpdatedAsync(int userId, decimal balance);

    /// <summary>A new bell notification, sent only to its owner (all their tabs and devices).</summary>
    Task NotificationAsync(int userId, NotificationDto notification);
}
