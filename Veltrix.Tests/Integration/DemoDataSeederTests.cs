using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Leaderboard;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>
/// The development demo data must obey the same rules as real play, on the real database
/// (unique ledger keys and the "balance never negative" CHECK constraint only exist in SQL Server).
/// </summary>
[Collection(ApiCollection.Name)]
public class DemoDataSeederTests
{
    private readonly VeltrixApiFactory _api;

    public DemoDataSeederTests(VeltrixApiFactory api) => _api = api;

    private async Task SeedAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var seeder = new DemoDataSeeder(
            services.GetRequiredService<ApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<TimeProvider>(),
            NullLogger.Instance);
        await seeder.SeedAsync();
    }

    [Fact]
    public async Task Demo_history_follows_the_game_rules_and_is_created_only_once()
    {
        await SeedAsync();
        var now = _api.Clock.UtcNow;
        var demoEmail = "@" + DemoDataSeeder.EmailDomain;

        await _api.WithDbAsync(async db =>
        {
            var users = await db.Users.Include(u => u.Wallet).Include(u => u.Stats)
                .Where(u => u.Email.EndsWith(demoEmail)).ToListAsync();
            Assert.Equal(15, users.Count);
            var ids = users.Select(u => u.Id).ToList();

            var bets = await db.Bets.Include(b => b.Round).Where(b => ids.Contains(b.UserId)).ToListAsync();
            Assert.True(bets.Count > 500, $"Only {bets.Count} demo bets");
            Assert.Contains(bets, b => b.Status == BetStatus.CashedOut);
            Assert.Contains(bets, b => b.Status == BetStatus.Lost);
            Assert.DoesNotContain(bets, b => b.Status == BetStatus.Active);

            foreach (var bet in bets)
            {
                var round = bet.Round!;
                Assert.Equal(RoundState.Finished, round.State);
                Assert.True(round.EndedAt <= now, "A demo round is dated in the future");

                // Provably fair: the crash point comes from the seed whose hash was published
                Assert.True(ProvablyFair.Verify(round.ServerSeed, round.ServerSeedHash, round.CrashPoint));

                if (bet.Status == BetStatus.CashedOut)
                {
                    Assert.True(bet.CashOutMultiplier < round.CrashPoint, $"Bet {bet.Id} cashed out at or above the crash point");
                    Assert.Equal(Math.Round(bet.Amount * bet.CashOutMultiplier!.Value, 2, MidpointRounding.ToZero), bet.Payout);
                }
            }

            foreach (var user in users)
            {
                // The ledger is one unbroken chain that ends at the wallet balance, and never goes below zero
                var ledger = await db.WalletTransactions.Where(t => t.WalletId == user.Wallet!.Id).OrderBy(t => t.Id).ToListAsync();
                for (var i = 1; i < ledger.Count; i++)
                    Assert.Equal(ledger[i - 1].BalanceAfter, ledger[i].BalanceBefore);
                Assert.All(ledger, t => Assert.True(t.BalanceAfter >= 0));
                Assert.Equal(ledger[^1].BalanceAfter, user.Wallet!.Balance);

                // PlayerStats (all-time leaderboard) agrees with the bets
                var own = bets.Where(b => b.UserId == user.Id).ToList();
                Assert.Equal(own.Count, user.Stats!.TotalBets);
                Assert.Equal(own.Count(b => b.Status == BetStatus.CashedOut), user.Stats.CashOutCount);
            }

            // The admin charts: stored days match the bets of that day
            var yesterday = DateOnly.FromDateTime(now).AddDays(-1);
            var start = yesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var stored = await db.DailyStatistics.SingleAsync(d => d.Date == yesterday);
            var actual = await db.Bets.CountAsync(b => b.PlacedAt >= start && b.PlacedAt < start.AddDays(1) && b.Status != BetStatus.Refunded);
            Assert.Equal(actual, stored.BetsCount);
            return 0;
        });

        // The leaderboard shows the demo players
        using var guest = _api.CreateApiClient();
        var board = await (await guest.GetAsync("api/leaderboard?period=All&type=Profit")).ReadAsync<LeaderboardDto>(HttpStatusCode.OK);
        Assert.NotEmpty(board.Entries);

        // A second start of the API adds nothing
        var before = await _api.WithDbAsync(db => db.Bets.CountAsync());
        await SeedAsync();
        Assert.Equal(before, await _api.WithDbAsync(db => db.Bets.CountAsync()));
    }
}
