using Microsoft.AspNetCore.SignalR;
using Veltrix.API.Hubs;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Realtime;

namespace Veltrix.API.Services;

/// <summary>SignalR implementation of IGameNotifier. IHubContext can send from anywhere (the engine, services).</summary>
public sealed class SignalRGameNotifier : IGameNotifier
{
    private readonly IHubContext<CrashHub> _hub;
    private readonly ILogger<SignalRGameNotifier> _logger;

    public SignalRGameNotifier(IHubContext<CrashHub> hub, ILogger<SignalRGameNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task RoundStateChangedAsync(string gameSlug, RoundStateChangedEvent payload) =>
        SendToGameAsync(gameSlug, "RoundStateChanged", payload);

    public Task MultiplierTickAsync(string gameSlug, MultiplierTickEvent payload) =>
        SendToGameAsync(gameSlug, "MultiplierTick", payload);

    public Task RoundCrashedAsync(string gameSlug, RoundCrashedEvent payload) =>
        SendToGameAsync(gameSlug, "RoundCrashed", payload);

    public Task BetPlacedAsync(string gameSlug, BetPlacedEvent payload) =>
        SendToGameAsync(gameSlug, "BetPlaced", payload);

    public Task PlayerCashedOutAsync(string gameSlug, PlayerCashedOutEvent payload) =>
        SendToGameAsync(gameSlug, "PlayerCashedOut", payload);

    /// <summary>Private event: only the wallet owner's connections (all their tabs/devices) receive it.</summary>
    public Task BalanceUpdatedAsync(int userId, decimal balance) =>
        SafeSendAsync(() => _hub.Clients.User(userId.ToString()).SendAsync("BalanceUpdated", new BalanceUpdatedEvent(balance)), "BalanceUpdated");

    private Task SendToGameAsync(string gameSlug, string method, object payload) =>
        SafeSendAsync(() => _hub.Clients.Group(CrashHub.GroupName(gameSlug)).SendAsync(method, payload), method);

    /// <summary>A failed push must never break the game or the bet that triggered it.</summary>
    private async Task SafeSendAsync(Func<Task> send, string method)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push {Method} failed", method);
        }
    }
}
