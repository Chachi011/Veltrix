using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Tests.Services;

/// <summary>Wallet and responsible play services: database effects + the live events they send (checked with Moq).</summary>
public class WalletAndLimitsServiceTests : IDisposable
{
    private readonly ServiceTestContext _ctx = new();

    private WalletService Wallet() => new(_ctx.Db, _ctx.CurrentUser.Object, _ctx.Notifier.Object, _ctx.Time, NullLogger<WalletService>.Instance);

    private LimitsService Limits() => new(_ctx.Db, _ctx.CurrentUser.Object, _ctx.Notifier.Object,
        new AchievementAwarder(_ctx.Db, _ctx.Notifier.Object, _ctx.Time, NullLogger<AchievementAwarder>.Instance),
        _ctx.Time, NullLogger<LimitsService>.Instance);

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task ClaimDailyBonus_CreditsTheSettingAmount_AndPushesTheNewBalance()
    {
        var player = _ctx.AddUser("bonus_player");
        _ctx.ActAs(player);

        var wallet = await Wallet().ClaimDailyBonusAsync();

        Assert.Equal(1100m, wallet.Balance); // 1000 start + 100 from the seeded setting
        Assert.False(wallet.CanClaimDailyBonus);
        _ctx.Notifier.Verify(n => n.BalanceUpdatedAsync(player.Id, 1100m), Times.Once);

        await using var fresh = _ctx.Fresh();
        Assert.Equal(2, await fresh.WalletTransactions.CountAsync(t => t.Wallet!.UserId == player.Id && t.Type == TransactionType.Bonus));
    }

    [Fact]
    public async Task ClaimDailyBonus_TwiceInADay_IsRefused_AndPushesNothingTheSecondTime()
    {
        var player = _ctx.AddUser("greedy");
        _ctx.ActAs(player);
        await Wallet().ClaimDailyBonusAsync();

        _ctx.Time.UtcNow = ServiceTestContext.Now.AddHours(23);
        await Assert.ThrowsAsync<DomainException>(() => Wallet().ClaimDailyBonusAsync());

        _ctx.Notifier.Verify(n => n.BalanceUpdatedAsync(player.Id, It.IsAny<decimal>()), Times.Once);
    }

    [Fact]
    public async Task SelfExclusion_StoresHistoryAndAudit_AndNotifiesThePlayerLive()
    {
        var player = _ctx.AddUser("pausing");
        _ctx.ActAs(player);

        var limits = await Limits().StartSelfExclusionAsync(new SelfExclusionRequest { Period = SelfExclusionPeriod.Days7 });

        Assert.True(limits.IsSelfExcluded);
        Assert.Equal(ServiceTestContext.Now.AddDays(7), limits.SelfExcludedUntil);
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.SelfExcluded)),
            Times.Once);

        await using var fresh = _ctx.Fresh();
        Assert.Equal(1, await fresh.SelfExclusions.CountAsync(e => e.UserId == player.Id));
        Assert.Equal(1, await fresh.AuditLogs.CountAsync(a => a.Action == AuditActions.SelfExclusionStarted));
        Assert.Equal(1, await fresh.Notifications.CountAsync(n => n.UserId == player.Id));
    }

    [Fact]
    public async Task RaisingTheLimit_IsShownAsPending_AndAuditedOnce()
    {
        var player = _ctx.AddUser("limited");
        _ctx.ActAs(player);
        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 50, SessionReminderMin = 60 });

        var limits = await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 500, SessionReminderMin = 60 });

        Assert.Equal(50m, limits.DailyBetLimit);
        Assert.True(limits.HasPendingLimitChange);
        Assert.Equal(500m, limits.PendingDailyBetLimit);
        Assert.Equal(ServiceTestContext.Now.AddHours(24), limits.PendingLimitAppliesAt);

        // Saving the same values again changes nothing, so no new audit row
        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 500, SessionReminderMin = 60 });
        await using var fresh = _ctx.Fresh();
        Assert.Equal(2, await fresh.AuditLogs.CountAsync(a => a.Action == AuditActions.LimitsChanged));

        // The only live events come from the "Set your limit" achievement of the FIRST save (once), not the limit itself:
        // the unlock, the level-up its 60 XP brings, and the balance with its VLX reward
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.AchievementUnlocked)), Times.Once);
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.LevelUp)), Times.Once);
        _ctx.Notifier.Verify(n => n.BalanceUpdatedAsync(player.Id, It.IsAny<decimal>()), Times.Once);
        _ctx.Notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SavingADailyLimit_CompletesTheSafetyTask_Once_WithItsRewardThroughTheLedger()
    {
        var player = _ctx.AddUser("careful");
        _ctx.ActAs(player);

        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 100, SessionReminderMin = 60 });
        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 80, SessionReminderMin = 60 });

        await using var fresh = _ctx.Fresh();
        var task = await fresh.Achievements.SingleAsync(a => a.Code == AchievementCodes.SetDailyLimit);
        Assert.Equal(1, await fresh.UserAchievements.CountAsync(u => u.UserId == player.Id && u.AchievementId == task.Id));
        Assert.Equal(task.XpReward, (await fresh.PlayerStats.SingleAsync(s => s.UserId == player.Id)).Xp);
        var reward = await fresh.WalletTransactions.SingleAsync(t => t.IdempotencyKey == $"achievement:{player.Id}:{AchievementCodes.SetDailyLimit}");
        Assert.Equal(task.VlxReward, reward.Amount);
        Assert.Equal(1000m + task.VlxReward, (await fresh.Wallets.SingleAsync(w => w.UserId == player.Id)).Balance);
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.AchievementUnlocked)),
            Times.Once);
    }

    [Fact]
    public async Task RemovingTheLimit_OrSavingOnlyTheReminder_CompletesNothing()
    {
        var player = _ctx.AddUser("no_limit");
        _ctx.ActAs(player);

        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = null, SessionReminderMin = 30 });

        await using var fresh = _ctx.Fresh();
        Assert.False(await fresh.UserAchievements.AnyAsync(u => u.UserId == player.Id));
        _ctx.Notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StaffSavingALimit_CollectNoAchievement()
    {
        var admin = _ctx.AddUser("limits_admin", UserRole.Admin);
        _ctx.ActAs(admin);

        await Limits().UpdateMyLimitsAsync(new UpdateLimitsRequest { DailyBetLimit = 100, SessionReminderMin = 60 });

        await using var fresh = _ctx.Fresh();
        Assert.False(await fresh.UserAchievements.AnyAsync(u => u.UserId == admin.Id));
    }
}
