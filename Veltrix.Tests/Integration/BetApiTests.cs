using System.Net;
using System.Net.Http.Json;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Auth;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>
/// Bets and cash-outs over real HTTP against SQL Server. The round is moved through its states with the engine's own
/// settlement code, so each test controls exactly when betting closes, the round starts and it crashes.
/// </summary>
[Collection(ApiCollection.Name)]
public class BetApiTests
{
    private readonly VeltrixApiFactory _api;

    public BetApiTests(VeltrixApiFactory api) => _api = api;

    private static Task<HttpResponseMessage> PlaceBetAsync(HttpClient client, decimal amount, int slot = 1) =>
        client.PostAsJsonAsync("api/bets", new PlaceBetRequest { GameSlug = VeltrixApiFactory.CrashSlug, Amount = amount, Slot = slot }, Json);

    private static Task<HttpResponseMessage> CashOutAsync(HttpClient client, int betId) =>
        client.PostAsync($"api/bets/{betId}/cashout", content: null);

    private static async Task<decimal> BalanceAsync(HttpClient client) =>
        (await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK)).Balance;

    [Fact]
    public async Task A_bet_while_betting_is_open_takes_the_stake_and_writes_a_ledger_row()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        await _api.OpenBettingRoundAsync();

        var result = await (await PlaceBetAsync(client, 25m)).ReadAsync<BetResultDto>(HttpStatusCode.OK);

        Assert.Equal(975m, result.Balance);
        Assert.Equal(BetStatus.Active, result.Bet.Status);
        Assert.Equal(975m, await BalanceAsync(client));

        // Every balance change has its ledger row with the balance before and after
        var ledger = await (await client.GetAsync("api/wallet/transactions")).ReadAsync<PagedResult<WalletTransactionDto>>(HttpStatusCode.OK);
        var row = Assert.Single(ledger.Items, t => t.Type == TransactionType.Bet);
        Assert.Equal(1000m, row.BalanceBefore);
        Assert.Equal(975m, row.BalanceAfter);
        Assert.Equal(result.Bet.Id, row.BetId);

        // While the round is not over, the crash point is a secret
        var myBets = await (await client.GetAsync("api/bets/my")).ReadAsync<PagedResult<BetDto>>(HttpStatusCode.OK);
        Assert.Null(Assert.Single(myBets.Items).CrashPoint);
    }

    [Fact]
    public async Task Bets_are_refused_after_betting_closes_and_for_invalid_amounts()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        var closed = await _api.OpenBettingRoundAsync();
        await _api.WithSettlementAsync(s => s.CloseBettingAsync(closed.Id, default));
        await (await PlaceBetAsync(client, 10m)).ReadProblemAsync(HttpStatusCode.BadRequest);

        await _api.OpenBettingRoundAsync();
        await (await PlaceBetAsync(client, 0.5m)).ReadProblemAsync(HttpStatusCode.BadRequest);   // below the game's minimum
        await (await PlaceBetAsync(client, 10.555m)).ReadProblemAsync(HttpStatusCode.BadRequest); // more than 2 decimals

        await (await PlaceBetAsync(client, 600m)).ShouldBeAsync(HttpStatusCode.OK);
        await (await PlaceBetAsync(client, 10m)).ReadProblemAsync(HttpStatusCode.Conflict);        // slot 1 already used
        await (await PlaceBetAsync(client, 500m, slot: 2)).ReadProblemAsync(HttpStatusCode.BadRequest); // only 400 left: never negative

        Assert.Equal(400m, await BalanceAsync(client));
    }

    [Fact]
    public async Task Cash_out_pays_the_stake_times_the_SERVER_multiplier_at_the_moment_of_the_request()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        var round = await _api.OpenBettingRoundAsync(minCrashPoint: 2m);
        var bet = (await (await PlaceBetAsync(client, 10m)).ReadAsync<BetResultDto>(HttpStatusCode.OK)).Bet;

        // Cashing out before the round runs is not possible
        await (await CashOutAsync(client, bet.Id)).ReadProblemAsync(HttpStatusCode.BadRequest);

        using (_api.Clock.Freeze())
        {
            await _api.WithSettlementAsync(s => s.CloseBettingAsync(round.Id, default));
            await _api.WithSettlementAsync(s => s.StartAsync(round.Id, default));

            var elapsed = MultiplierCurve.TimeToReach(1.2m);
            _api.Clock.Advance(elapsed);

            // The request carries no multiplier at all: the server computes it from its own clock
            var result = await (await CashOutAsync(client, bet.Id)).ReadAsync<BetResultDto>(HttpStatusCode.OK);

            var serverMultiplier = MultiplierCurve.At(elapsed);
            Assert.Equal(BetStatus.CashedOut, result.Bet.Status);
            Assert.Equal(serverMultiplier, result.Bet.CashOutMultiplier);
            Assert.Equal(Math.Round(10m * serverMultiplier, 2, MidpointRounding.ToZero), result.Bet.Payout);
            Assert.Equal(990m + result.Bet.Payout, result.Balance);

            // A bet is paid once
            await (await CashOutAsync(client, bet.Id)).ReadProblemAsync(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Cash_out_after_the_crash_point_is_refused_and_the_crash_settles_the_bet_as_lost()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        var round = await _api.OpenBettingRoundAsync(maxCrashPoint: 1.2m);
        var bet = (await (await PlaceBetAsync(client, 10m)).ReadAsync<BetResultDto>(HttpStatusCode.OK)).Bet;

        using (_api.Clock.Freeze())
        {
            await _api.WithSettlementAsync(s => s.CloseBettingAsync(round.Id, default));
            var started = await _api.WithSettlementAsync(s => s.StartAsync(round.Id, default));

            // A moment after the multiplier passed the crash point (the engine has not processed the crash yet)
            _api.Clock.Advance(started.CrashesAt!.Value - _api.Clock.UtcNow + TimeSpan.FromMilliseconds(50));
            var problem = await (await CashOutAsync(client, bet.Id)).ReadProblemAsync(HttpStatusCode.BadRequest);
            Assert.Contains("Too late", problem.GetProperty("detail").GetString());

            await _api.WithSettlementAsync(s => s.CrashAsync(round.Id, default));
            await _api.WithSettlementAsync(s => s.FinishAsync(round.Id, default));
        }

        var myBets = await (await client.GetAsync("api/bets/my")).ReadAsync<PagedResult<BetDto>>(HttpStatusCode.OK);
        var settled = Assert.Single(myBets.Items);
        Assert.Equal(BetStatus.Lost, settled.Status);
        Assert.Equal(round.CrashPoint, settled.CrashPoint); // revealed now that the round is over
        Assert.Equal(990m, await BalanceAsync(client));
    }

    [Fact]
    public async Task An_operator_cancelling_a_round_refunds_active_bets_but_cashed_out_bets_keep_their_payout()
    {
        var cashier = await _api.RegisterPlayerAsync();
        var waiter = await _api.RegisterPlayerAsync();
        using var cashierClient = _api.ClientFor(cashier);
        using var waiterClient = _api.ClientFor(waiter);
        var round = await _api.OpenBettingRoundAsync(minCrashPoint: 1.5m);

        var cashierBet = (await (await PlaceBetAsync(cashierClient, 100m)).ReadAsync<BetResultDto>(HttpStatusCode.OK)).Bet;
        await (await PlaceBetAsync(waiterClient, 50m)).ShouldBeAsync(HttpStatusCode.OK);

        decimal payout;
        using (_api.Clock.Freeze())
        {
            await _api.WithSettlementAsync(s => s.CloseBettingAsync(round.Id, default));
            await _api.WithSettlementAsync(s => s.StartAsync(round.Id, default));
            _api.Clock.Advance(MultiplierCurve.TimeToReach(1.1m));
            payout = (await (await CashOutAsync(cashierClient, cashierBet.Id)).ReadAsync<BetResultDto>(HttpStatusCode.OK)).Bet.Payout!.Value;
        }

        var operatorAuth = await _api.LoginSeededAsync("operator");
        using var operatorClient = _api.ClientFor(operatorAuth);
        var cancellation = await (await operatorClient.PostAsJsonAsync($"api/rounds/{round.Id}/cancel",
                new CancelRoundRequest { Reason = "Integration test: server problem" }, Json))
            .ReadAsync<RoundCancellationDto>(HttpStatusCode.OK);

        Assert.Equal(1, cancellation.RefundedBetsCount);
        Assert.Equal(1000m, await BalanceAsync(waiterClient));
        Assert.Equal(900m + payout, await BalanceAsync(cashierClient));
    }

    [Fact]
    public async Task Only_players_can_bet()
    {
        await _api.OpenBettingRoundAsync();

        using var guest = _api.CreateApiClient();
        await (await PlaceBetAsync(guest, 10m)).ReadProblemAsync(HttpStatusCode.Unauthorized);

        foreach (var staff in new[] { "operator", "admin" })
        {
            using var client = _api.ClientFor(await _api.LoginSeededAsync(staff));
            await (await PlaceBetAsync(client, 10m)).ReadProblemAsync(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task A_blocked_player_cannot_bet_or_refresh_even_with_a_token_issued_before_the_block()
    {
        var player = await _api.RegisterPlayerAsync();
        using var playerClient = _api.ClientFor(player);
        await _api.OpenBettingRoundAsync();

        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));
        await (await admin.PutAsJsonAsync($"api/admin/users/{player.User.Id}/block", new BlockUserRequest { IsBlocked = true }, Json))
            .ShouldBeAsync(HttpStatusCode.OK);

        // The access token is still valid for up to 15 minutes, so the bet endpoint checks the block itself
        await (await PlaceBetAsync(playerClient, 10m)).ReadProblemAsync(HttpStatusCode.Forbidden);

        using var anonymous = _api.CreateApiClient();
        await (await anonymous.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = player.RefreshToken }, Json))
            .ReadProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_self_excluded_player_cannot_bet()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        await _api.OpenBettingRoundAsync();

        var limits = await (await client.PostAsJsonAsync("api/limits/self-exclusion", new SelfExclusionRequest { Period = SelfExclusionPeriod.Hours24 }, Json))
            .ReadAsync<PlayerLimitsDto>(HttpStatusCode.OK);
        Assert.NotNull(limits.SelfExcludedUntil);

        await (await PlaceBetAsync(client, 10m)).ReadProblemAsync(HttpStatusCode.Forbidden);
        Assert.Equal(1000m, await BalanceAsync(client));
    }
}
