using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Achievements;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>
/// Achievements over real HTTP. Play tasks are unlocked by the round settlement, Learn and Safety tasks by their action
/// (verify a round, read the rules, save a daily limit); rewards go through the wallet ledger; players who cannot play
/// right now see no task that asks them to play (responsible play).
/// </summary>
[Collection(ApiCollection.Name)]
public class AchievementsApiTests
{
    private readonly VeltrixApiFactory _api;

    public AchievementsApiTests(VeltrixApiFactory api) => _api = api;

    private static Task<HttpResponseMessage> PlaceBetAsync(HttpClient client, decimal amount, decimal? auto = null) =>
        client.PostAsJsonAsync("api/bets", new PlaceBetRequest { GameSlug = VeltrixApiFactory.CrashSlug, Amount = amount, AutoCashOutAt = auto }, Json);

    /// <summary>A round where the player loses: betting → running → crash → finished.</summary>
    private async Task PlayALostRoundAsync(HttpClient client, decimal amount = 10m, decimal? auto = null)
    {
        var round = await _api.OpenBettingRoundAsync(maxCrashPoint: 1.2m);
        await (await PlaceBetAsync(client, amount, auto)).ShouldBeAsync(HttpStatusCode.OK);
        await FinishRoundAsync(round.Id);
    }

    private async Task FinishRoundAsync(int roundId)
    {
        await _api.WithSettlementAsync(s => s.CloseBettingAsync(roundId, default));
        await _api.WithSettlementAsync(s => s.StartAsync(roundId, default));
        await _api.WithSettlementAsync(s => s.CrashAsync(roundId, default));
        await _api.WithSettlementAsync(s => s.FinishAsync(roundId, default));
    }

    private static async Task<MyAchievementsDto> MineAsync(HttpClient client) =>
        await (await client.GetAsync("api/achievements/my")).ReadAsync<MyAchievementsDto>(HttpStatusCode.OK);

    private static async Task<bool> CanPlayNowAsync(HttpClient client) =>
        (await (await client.GetAsync("api/limits")).ReadAsync<PlayerLimitsDto>(HttpStatusCode.OK)).CanPlayNow;

    private static async Task<List<string>> UnlockedAsync(HttpClient client) =>
        (await MineAsync(client)).Items.Where(a => a.UnlockedAt is not null).Select(a => a.Code).Order().ToList();

    [Fact]
    public async Task The_catalog_is_public_has_only_the_responsible_tasks_and_shows_safety_and_learning_first()
    {
        using var guest = _api.CreateApiClient();
        var all = await (await guest.GetAsync("api/achievements")).ReadAsync<List<AchievementDto>>(HttpStatusCode.OK);

        Assert.Equal(
            new[] { "SET_DAILY_LIMIT", "READ_RULES", "VERIFY_ROUND", "FIRST_BET", "FIRST_CASHOUT", "AUTO_CASHOUT" },
            all.Select(a => a.Code));
        Assert.Equal(AchievementKind.Safety, all[0].Kind);
        Assert.DoesNotContain(all, a => AchievementCodes.Retired.Contains(a.Code));

        await (await guest.GetAsync("api/achievements/my")).ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_first_settled_bet_unlocks_Try_Crash_once_and_the_bet_itself_gives_no_XP()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        await PlayALostRoundAsync(client);

        // 1000 − 10 (lost bet) + 10 (Try Crash reward), paid with its own ledger row
        var wallet = await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK);
        Assert.Equal(1000m, wallet.Balance);
        var ledger = await (await client.GetAsync("api/wallet/transactions")).ReadAsync<PagedResult<WalletTransactionDto>>(HttpStatusCode.OK);
        var reward = Assert.Single(ledger.Items, t => t.Reason == "Achievement: Try Crash");
        Assert.Equal(TransactionType.Bonus, reward.Type);
        Assert.Equal(10m, reward.Amount);

        // XP: only the 10 of the achievement, nothing for the bet → level 2 (10 XP), level 3 needs 30
        var profile = await (await client.GetAsync($"api/players/{player.User.Nickname}")).ReadAsync<PublicProfileDto>(HttpStatusCode.OK);
        Assert.Equal(10, profile.Xp);
        Assert.Equal(2, profile.Level);
        Assert.Equal(3, profile.NextLevel);
        Assert.Equal(30, profile.NextLevelXp);
        Assert.Equal("FIRST_BET", Assert.Single(profile.Badges).Code);

        var notifications = await (await client.GetAsync("api/notifications")).ReadAsync<NotificationPageDto>(HttpStatusCode.OK);
        Assert.Contains(notifications.Items, n => n.Type == "AchievementUnlocked" && n.Message.StartsWith("Try Crash"));

        // More rounds: no second reward and no XP for playing more
        await PlayALostRoundAsync(client);
        await PlayALostRoundAsync(client, amount: 500m);
        var rewards = await _api.WithDbAsync(db =>
            db.WalletTransactions.CountAsync(t => t.Wallet!.UserId == player.User.Id && t.IdempotencyKey.StartsWith("achievement:")));
        Assert.Equal(1, rewards);
        var after = await (await client.GetAsync($"api/players/{player.User.Nickname}")).ReadAsync<PublicProfileDto>(HttpStatusCode.OK);
        Assert.Equal(10, after.Xp);
    }

    [Fact]
    public async Task A_bet_with_an_auto_target_completes_Plan_ahead_even_when_it_loses()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        await PlayALostRoundAsync(client, auto: 5m);

        Assert.Equal(new[] { "AUTO_CASHOUT", "FIRST_BET" }, await UnlockedAsync(client));
    }

    [Fact]
    public async Task Winning_with_auto_cash_out_unlocks_the_three_play_tasks_at_once()
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

            // What the engine pushes live after the crash: the new balance, three unlocks and one level-up (50 XP = level 4)
            Assert.Equal(3, crash.Notifications.Count(n => n.Type == "AchievementUnlocked"));
            Assert.Single(crash.Notifications, n => n.Type == "LevelUp");
            Assert.Single(crash.Balances, b => b.UserId == player.User.Id);
        }

        Assert.Equal(new[] { "AUTO_CASHOUT", "FIRST_BET", "FIRST_CASHOUT" }, await UnlockedAsync(client));

        // 1000 − 10 + 15 (payout) + 10 + 20 + 10 (rewards)
        var wallet = await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK);
        Assert.Equal(1045m, wallet.Balance);
    }

    [Fact]
    public async Task Verifying_a_real_finished_round_completes_Check_a_round_but_a_made_up_seed_does_not()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        var round = await _api.OpenBettingRoundAsync();
        await FinishRoundAsync(round.Id);
        var revealed = await (await client.GetAsync($"api/fairness/rounds/{round.Id}")).ReadAsync<FairnessRoundDto>(HttpStatusCode.OK);

        // A seed that belongs to no round: the calculation works, the task is not completed
        var madeUp = new VerifyFairnessRequest { ServerSeed = new string('a', 64) };
        await (await client.PostAsJsonAsync("api/fairness/verify", madeUp, Json)).ShouldBeAsync(HttpStatusCode.OK);
        Assert.Empty(await UnlockedAsync(client));

        var real = new VerifyFairnessRequest { ServerSeed = revealed.ServerSeed!, ServerSeedHash = revealed.ServerSeedHash, CrashPoint = revealed.CrashPoint };
        var result = await (await client.PostAsJsonAsync("api/fairness/verify", real, Json)).ReadAsync<VerifyFairnessResult>(HttpStatusCode.OK);
        Assert.True(result.HashMatches);
        Assert.Equal(new[] { "VERIFY_ROUND" }, await UnlockedAsync(client));

        // Guests can still verify (no account needed), and staff collect nothing
        using var guest = _api.CreateApiClient();
        await (await guest.PostAsJsonAsync("api/fairness/verify", real, Json)).ShouldBeAsync(HttpStatusCode.OK);
        var admin = await _api.LoginSeededAsync("admin");
        using var adminClient = _api.ClientFor(admin);
        await (await adminClient.PostAsJsonAsync("api/fairness/verify", real, Json)).ShouldBeAsync(HttpStatusCode.OK);
        Assert.False(await _api.WithDbAsync(db => db.UserAchievements.AnyAsync(u => u.UserId == admin.User.Id)));
    }

    [Fact]
    public async Task Reading_the_rules_to_the_end_completes_Know_the_rules()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        await (await client.PostAsync("api/pages/rules/read", null)).ShouldBeAsync(HttpStatusCode.NoContent);
        Assert.Empty(await UnlockedAsync(client)); // the first page alone is not the end

        await (await client.PostAsync("api/pages/no-winning-strategy/read", null)).ShouldBeAsync(HttpStatusCode.NoContent);
        await (await client.PostAsync("api/pages/no-winning-strategy/read", null)).ShouldBeAsync(HttpStatusCode.NoContent); // again: nothing new
        Assert.Equal(new[] { "READ_RULES" }, await UnlockedAsync(client));
        Assert.Equal(1, await _api.WithDbAsync(db =>
            db.WalletTransactions.CountAsync(t => t.IdempotencyKey == $"achievement:{player.User.Id}:READ_RULES")));

        await (await client.PostAsync("api/pages/no-such-page/read", null)).ReadProblemAsync(HttpStatusCode.NotFound);
        using var guest = _api.CreateApiClient();
        await (await guest.PostAsync("api/pages/no-winning-strategy/read", null)).ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Saving_a_daily_limit_completes_Set_your_limit()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);

        await (await client.PutAsJsonAsync("api/limits", new UpdateLimitsRequest { DailyBetLimit = 200m, SessionReminderMin = 60 }, Json))
            .ShouldBeAsync(HttpStatusCode.OK);

        Assert.Equal(new[] { "SET_DAILY_LIMIT" }, await UnlockedAsync(client));
        var profile = await (await client.GetAsync($"api/players/{player.User.Nickname}")).ReadAsync<PublicProfileDto>(HttpStatusCode.OK);
        Assert.Equal(60, profile.Xp); // the biggest single reward is the safety task
    }

    [Fact]
    public async Task A_self_excluded_player_sees_no_task_that_asks_to_play()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        Assert.False((await MineAsync(client)).PlayTasksHidden);
        Assert.True(await CanPlayNowAsync(client));

        await (await client.PostAsJsonAsync("api/limits/self-exclusion", new SelfExclusionRequest { Period = SelfExclusionPeriod.Hours24 }, Json))
            .ShouldBeAsync(HttpStatusCode.OK);

        // The client hides every invitation to play (Play buttons, the bet button) from this flag
        Assert.False(await CanPlayNowAsync(client));
        var mine = await MineAsync(client);
        Assert.True(mine.PlayTasksHidden);
        Assert.DoesNotContain(mine.Items, a => a.Kind == AchievementKind.Play);
        Assert.Equal(new[] { "SET_DAILY_LIMIT", "READ_RULES", "VERIFY_ROUND" }, mine.Items.Select(a => a.Code)); // learning and safety stay
    }

    [Fact]
    public async Task A_player_who_used_up_the_daily_limit_sees_no_task_that_asks_to_play()
    {
        var player = await _api.RegisterPlayerAsync();
        using var client = _api.ClientFor(player);
        await (await client.PutAsJsonAsync("api/limits", new UpdateLimitsRequest { DailyBetLimit = 10m, SessionReminderMin = 60 }, Json))
            .ShouldBeAsync(HttpStatusCode.OK);
        Assert.False((await MineAsync(client)).PlayTasksHidden); // 10 VLX left: the play tasks are still there
        Assert.True(await CanPlayNowAsync(client));

        await PlayALostRoundAsync(client, amount: 10m);

        Assert.False(await CanPlayNowAsync(client));
        var mine = await MineAsync(client);
        Assert.True(mine.PlayTasksHidden);
        // "Try Crash" is done, so it stays (it no longer asks anything); first cash-out and plan ahead are hidden
        Assert.Equal(
            new[] { "FIRST_BET" },
            mine.Items.Where(a => a.Kind == AchievementKind.Play).Select(a => a.Code));
        Assert.NotNull(mine.Items.Single(a => a.Code == "FIRST_BET").UnlockedAt);
    }
}
