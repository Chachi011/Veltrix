using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.DTOs.Realtime;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Hubs;

/// <summary>
/// Real-time channel at /hubs/crash. Guests may connect too (to watch rounds live);
/// a logged-in user passes the JWT as ?access_token=... and additionally receives BalanceUpdated.
///
/// Clients only JOIN/LEAVE a game here. Bets and cash-outs go through the REST API,
/// so they get the same validation, rate limiting and ProblemDetails errors as everything else.
/// </summary>
[AllowAnonymous]
public sealed class CrashHub : Hub
{
    // connectionId -> nothing; a concurrent "set" of open connections across all hub instances
    private static readonly ConcurrentDictionary<string, byte> Connections = new();

    private readonly IRoundService _roundService;
    private readonly IGameService _gameService;

    public CrashHub(IRoundService roundService, IGameService gameService)
    {
        _roundService = roundService;
        _gameService = gameService;
    }

    public static string GroupName(string gameSlug) => $"game:{gameSlug}";

    public static int OnlineCount => Connections.Count;

    public override async Task OnConnectedAsync()
    {
        Connections.TryAdd(Context.ConnectionId, 0);
        await Clients.All.SendAsync("OnlineCount", new OnlineCountEvent(OnlineCount));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Connections.TryRemove(Context.ConnectionId, out _);
        await Clients.All.SendAsync("OnlineCount", new OnlineCountEvent(OnlineCount));
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Start receiving the events of one game. The caller immediately gets the current round state.</summary>
    public async Task JoinGame(string slug)
    {
        try
        {
            await _gameService.GetBySlugAsync(slug);
        }
        catch (NotFoundException)
        {
            throw new HubException($"Game '{slug}' was not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(slug));

        // A client that joins in the middle of a round must not wait for the next event to know what is happening
        try
        {
            var round = await _roundService.GetCurrentAsync(slug);
            await Clients.Caller.SendAsync("RoundStateChanged", new RoundStateChangedEvent(
                round.Id, round.RoundNumber, round.State, round.ServerSeedHash, round.BettingEndsAt, round.StartedAt));
        }
        catch (NotFoundException)
        {
            // No round has been played yet; the next RoundStateChanged will arrive soon
        }
    }

    public Task LeaveGame(string slug) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(slug));
}
