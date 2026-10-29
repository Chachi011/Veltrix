using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Achievements;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>Achievements and XP over real HTTP: unlocked by the round settlement, paid through the wallet ledger, shown in the API.</summary>
[Collection(ApiCollection.Name)]
public class AchievementsApiTests
{
    private readonly VeltrixApiFactory _api;

    public AchievementsApiTests(VeltrixApiFactory api) => _api = api;

    private static Task<HttpResponseMessage> PlaceBetAsync(HttpClient client, decimal amount, decimal? auto = null) =>
        client.PostAsJsonAsync("api/bets", new PlaceBetRequest { GameSlug = VeltrixApiFactory.CrashSlug, Amount = amount, AutoCashOutAt = auto }, Json);

    /// <summary>A round where the player loses 10 VLX: betting → running → crash → finished.</summary>
    private async Task PlayALostRoundAsync(HttpClient client)
    {
        var round = await _api.OpenBettingRoundAsync(maxCrashPoint: 1.2m);
        await (await PlaceBetAsync(client, 10m)).ShouldBeAsync(HttpStatusCode.OK);
        await _api.WithSettlementAsync(s => s.CloseBettingAsync(round.Id, default));
        await _api.WithSettlementAsync(s => s.StartAsync(round.Id, default));
        await _api.WithSettlementAsync(s => s.CrashAsync(round.Id, default));
        await _api.WithSettlementAsync(s => s.FinishAsync(round.Id, default));
    }

    [Fact]
    public async Task The_catalog_is_public()
    {
        using var guest = _api.CreateApiClient();
        var all = await (await guest.GetAsync("api/achievements")).ReadAsync<List<AchievementDto>>(HttpStatusCode.OK);

        Assert.Equal(7, all.Count);
        Assert.Contains(all, a => a.Code == "FIRST_BET" && a.XpReward == 10 && a.VlxReward == 10);

        await (await guest.GetAsync("api/achievements/my")).ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_first_settled_bet_unlocks_First_bet_once_with_its_XP_VLX_reward_and_a_notification()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        await PlayALostRoundAsync(client);

        // 1000 − 10 (lost bet) + 10 (First bet reward), paid with its own ledger row
        var wallet = await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK);
        Assert.Equal(1000m, wallet.Balance);
        var ledger = await (await client.GetAsync("api/wallet/transactions")).ReadAsync<PagedResult<WalletTransactionDto>>(HttpStatusCode.OK);
        var reward = Assert.Single(ledger.Items, t => t.Reason == "Achievement: First bet");
        Assert.Equal(TransactionType.Bonus, reward.Type);
        Assert.Equal(10m, reward.Amount);

        // XP: 1 for the lost bet + 10 from the achievement
        var profile = await (await client.GetAsync($"api/players/{player.User.Nickname}")).ReadAsync<PublicProfileDto>(HttpStatusCode.OK);
        Assert.Equal(11, profile.Xp);
        Assert.Equal(1, profile.Level);
        Assert.Equal(2, profile.NextLevel);
        Assert.Equal(100, profile.NextLevelXp);
        Assert.Equal("FIRST_BET", Assert.Single(profile.Badges).Code);

        var notifications = await (await client.GetAsync("api/notifications")).ReadAsync<NotificationPageDto>(HttpStatusCode.OK);
        Assert.Contains(notifications.Items, n => n.Type == "AchievementUnlocked" && n.Message.StartsWith("First bet"));

        // A second round: no second reward
        await PlayALostRoundAsync(client);
        var rewards = await _api.WithDbAsync(db =>
            db.WalletTransactions.CountAsync(t => t.Wallet!.UserId == player.User.Id && t.IdempotencyKey.StartsWith("achievement:")));
        Assert.Equal(1, rewards);
    }

    [Fact]
    public async Task My_achievements_show_unlocked_ones_and_the_progress_of_the_others()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        await PlayALostRoundAsync(client);

        var mine = await (await client.GetAsync("api/achievements/my")).ReadAsync<List<MyAchievementDto>>(HttpStatusCode.OK);

        var firstBet = Assert.Single(mine, a => a.Code == "FIRST_BET");
        Assert.NotNull(firstBet.UnlockedAt);
        var regular = Assert.Single(mine, a => a.Code == "BETS_100");
        Assert.Null(regular.UnlockedAt);
        Assert.Equal(1m, regular.Progress);
        Assert.Equal(100m, regular.Target);
    }

    [Fact]
    public async Task Winning_with_auto_cash_out_unlocks_three_achievements_at_once()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        var round = await _api.OpenBettingRoundAsync(minCrashPoint: 2m);
        var bet = (await (await PlaceBetAsync(client, 10m, auto: 1.5m)).ReadAsync<BetResultDto>(HttpStatusCode.OK)).Bet;

        using (_api.Clock.Freeze())
        {
            await _api.WithSettlementAsync(s => s.CloseBettingAsync(round.Id, default));
            await _api.WithSettlementAsync(s => s.StartAsync(round.Id, default));
            _api.Clock.Advance(MultiplierCurve.TimeToReach(1.5m));
            await _api.WithSettlementAsync(s => s.AutoCashOutAsync(bet.Id, 1.5m, default)); // what the engine does at 1.50x
            var crash = await _api.WithSettlementAsync(s => s.CrashAsync(round.Id, default));

            // What the engine pushes live after the crash: the new balance and three bell notifications
            Assert.Equal(3, crash.Notifications.Count);
            Assert.Single(crash.Balances, b => b.UserId == player.User.Id);
        }

        var mine = await (await client.GetAsync("api/achievements/my")).ReadAsync<List<MyAchievementDto>>(HttpStatusCode.OK);
        var unlocked = mine.Where(a => a.UnlockedAt is not null).Select(a => a.Code).Order().ToList();
        Assert.Equal(new[] { "AUTO_CASHOUT", "FIRST_BET", "FIRST_CASHOUT" }, unlocked);

        // 1000 − 10 + 15 (payout) + 10 + 20 + 10 (rewards)
        var wallet = await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK);
        Assert.Equal(1045m, wallet.Balance);
    }
}
