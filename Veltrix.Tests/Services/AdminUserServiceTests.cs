using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Tests.Services;

/// <summary>Admin actions: one transaction for the change + audit + notification, then the live pushes.</summary>
public class AdminUserServiceTests : IDisposable
{
    private readonly ServiceTestContext _ctx = new();

    private AdminUserService Service() =>
        new(_ctx.Db, _ctx.CurrentUser.Object, _ctx.Notifier.Object, _ctx.Time, NullLogger<AdminUserService>.Instance);

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task AdjustBalance_CreditsTheWallet_WritesTheAudit_AndNotifiesThePlayerLive()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        var player = _ctx.AddUser("lucky");
        _ctx.ActAs(admin);

        var result = await Service().AdjustBalanceAsync(player.Id,
            new BalanceAdjustmentRequest { Amount = 250, Reason = "Event prize", RequestId = Guid.NewGuid() });

        Assert.Equal(1000m, result.BalanceBefore);
        Assert.Equal(1250m, result.BalanceAfter);
        Assert.Equal("boss", result.AdminNickname);

        _ctx.Notifier.Verify(n => n.BalanceUpdatedAsync(player.Id, 1250m), Times.Once);
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.BalanceAdjusted && d.Message.Contains("Event prize"))),
            Times.Once);

        await using var fresh = _ctx.Fresh();
        var tx = await fresh.WalletTransactions.SingleAsync(t => t.Type == TransactionType.AdminAdjustment);
        Assert.Equal(250m, tx.Amount);
        Assert.Equal("Event prize", tx.Reason);
        Assert.Equal(1, await fresh.BalanceAdjustments.CountAsync());
        Assert.Equal(1, await fresh.AuditLogs.CountAsync(a => a.Action == AuditActions.BalanceAdjusted && a.ActorUserId == admin.Id));
    }

    [Fact]
    public async Task AdjustBalance_TheSameRequestIdTwice_IsAppliedOnlyOnce()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        var player = _ctx.AddUser("lucky");
        _ctx.ActAs(admin);
        var request = new BalanceAdjustmentRequest { Amount = 100, Reason = "Double click test", RequestId = Guid.NewGuid() };

        await Service().AdjustBalanceAsync(player.Id, request);
        await Assert.ThrowsAsync<ConflictException>(() => Service().AdjustBalanceAsync(player.Id, request));

        await using var fresh = _ctx.Fresh();
        Assert.Equal(1100m, (await fresh.Wallets.SingleAsync(w => w.UserId == player.Id)).Balance);
        _ctx.Notifier.Verify(n => n.BalanceUpdatedAsync(player.Id, It.IsAny<decimal>()), Times.Once);
    }

    [Fact]
    public async Task AdjustBalance_OfYourOwnWallet_IsRefused_AndNothingIsPushed()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        _ctx.ActAs(admin);

        await Assert.ThrowsAsync<DomainException>(() =>
            Service().AdjustBalanceAsync(admin.Id, new BalanceAdjustmentRequest { Amount = 1000, Reason = "Paying myself" }));

        _ctx.Notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeRole_SavesTheAudit_AndTellsTheUserLive()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        var player = _ctx.AddUser("promoted");
        _ctx.ActAs(admin);

        var details = await Service().ChangeRoleAsync(player.Id, new ChangeRoleRequest { Role = UserRole.Operator });

        Assert.Equal(UserRole.Operator, details.Role);
        _ctx.Notifier.Verify(
            n => n.NotificationAsync(player.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.RoleChanged)),
            Times.Once);

        await using var fresh = _ctx.Fresh();
        var audit = await fresh.AuditLogs.SingleAsync(a => a.Action == AuditActions.UserRoleChanged);
        Assert.Contains("Player", audit.OldValue);
        Assert.Contains("Operator", audit.NewValue);
    }

    [Fact]
    public async Task ChangeRole_ToTheSameRole_ChangesNothing_AndSendsNothing()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        var player = _ctx.AddUser("same");
        _ctx.ActAs(admin);

        await Service().ChangeRoleAsync(player.Id, new ChangeRoleRequest { Role = UserRole.Player });

        _ctx.Notifier.VerifyNoOtherCalls();
        await using var fresh = _ctx.Fresh();
        Assert.Equal(0, await fresh.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task UsersList_FiltersBySearchAndRole()
    {
        var admin = _ctx.AddUser("boss", UserRole.Admin);
        _ctx.AddUser("alpha_one");
        _ctx.AddUser("alpha_two");
        _ctx.AddUser("beta");
        _ctx.ActAs(admin);

        var alphas = await Service().GetUsersAsync(new AdminUserQuery { Search = "alpha", Page = 1, PageSize = 10 });
        var admins = await Service().GetUsersAsync(new AdminUserQuery { Role = UserRole.Admin, Page = 1, PageSize = 10 });

        Assert.Equal(2, alphas.TotalCount);
        Assert.All(alphas.Items, u => Assert.StartsWith("alpha", u.Nickname));
        Assert.Equal("boss", Assert.Single(admins.Items).Nickname);
    }
}
