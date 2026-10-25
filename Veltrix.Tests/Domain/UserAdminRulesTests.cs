using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

/// <summary>Role changes and blocking: an admin can never lock themselves out.</summary>
public class UserAdminRulesTests
{
    private const int AdminId = 99;

    private static User Player() => new() { Id = 5, Role = UserRole.Player };

    [Fact]
    public void ChangeRole_ChangesTheRole()
    {
        var user = Player();

        Assert.True(user.ChangeRole(UserRole.Operator, AdminId));
        Assert.Equal(UserRole.Operator, user.Role);
    }

    [Fact]
    public void ChangeRole_ToTheSameRole_ChangesNothing()
    {
        var user = Player();

        Assert.False(user.ChangeRole(UserRole.Player, AdminId));
    }

    [Fact]
    public void ChangeRole_OfYourself_IsRefused()
    {
        var admin = new User { Id = AdminId, Role = UserRole.Admin };

        Assert.Throws<DomainException>(() => admin.ChangeRole(UserRole.Player, AdminId));
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public void ChangeRole_ToAnUnknownValue_IsRefused()
    {
        var user = Player();

        Assert.Throws<DomainException>(() => user.ChangeRole((UserRole)42, AdminId));
        Assert.Equal(UserRole.Player, user.Role);
    }

    [Fact]
    public void Block_BlocksTheUser_Once()
    {
        var user = Player();

        Assert.True(user.Block(AdminId));
        Assert.True(user.IsBlocked);
        Assert.False(user.Block(AdminId));
    }

    [Fact]
    public void Block_Yourself_IsRefused()
    {
        var admin = new User { Id = AdminId, Role = UserRole.Admin };

        Assert.Throws<DomainException>(() => admin.Block(AdminId));
        Assert.False(admin.IsBlocked);
    }

    [Fact]
    public void Unblock_AlsoClearsALoginLockout()
    {
        var user = Player();
        user.Block(AdminId);
        user.FailedLoginCount = 3;
        user.LockoutUntil = TestData.Now.AddMinutes(10);

        Assert.True(user.Unblock());

        Assert.False(user.IsBlocked);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutUntil);
    }

    [Fact]
    public void Unblock_ANotBlockedUser_ChangesNothing()
    {
        Assert.False(Player().Unblock());
    }
}
