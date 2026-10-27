using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

/// <summary>Admin balance adjustment: required reason, never negative, never your own wallet.</summary>
public class BalanceAdjustmentTests
{
    private static readonly DateTime Now = TestData.Now;
    private const int AdminId = 99;

    [Fact]
    public void PositiveAdjustment_AddsPoints_WithAnAdminAdjustmentLedgerRow()
    {
        var wallet = TestData.Wallet(100m);

        var adjustment = BalanceAdjustment.Apply(wallet, AdminId, 50m, "Compensation for a lag", "admin-adjust:1", Now);

        Assert.Equal(150m, wallet.Balance);
        var transaction = Assert.Single(wallet.Transactions);
        Assert.Equal(TransactionType.AdminAdjustment, transaction.Type);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal(100m, transaction.BalanceBefore);
        Assert.Equal(150m, transaction.BalanceAfter);
        Assert.Equal("Compensation for a lag", transaction.Reason);

        Assert.Same(transaction, adjustment.WalletTransaction);
        Assert.Equal(wallet.UserId, adjustment.UserId);
        Assert.Equal(AdminId, adjustment.AdminUserId);
        Assert.Equal(50m, adjustment.Amount);
        Assert.Equal(Now, adjustment.CreatedAt);
    }

    [Fact]
    public void NegativeAdjustment_RemovesPoints()
    {
        var wallet = TestData.Wallet(100m);

        var adjustment = BalanceAdjustment.Apply(wallet, AdminId, -30m, "Duplicate bonus removed", "admin-adjust:1", Now);

        Assert.Equal(70m, wallet.Balance);
        Assert.Equal(-30m, adjustment.Amount);
        Assert.Equal(-30m, adjustment.WalletTransaction!.Amount);
        Assert.Equal(70m, adjustment.WalletTransaction.BalanceAfter);
    }

    [Fact]
    public void RemovingMoreThanTheBalance_IsRefused_AndChangesNothing()
    {
        var wallet = TestData.Wallet(50m);

        var error = Assert.Throws<DomainException>(() =>
            BalanceAdjustment.Apply(wallet, AdminId, -50.01m, "Correction", "admin-adjust:1", Now));

        Assert.Contains("never go negative", error.Message);
        Assert.Equal(50m, wallet.Balance);
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void RemovingTheWholeBalance_LeavesExactlyZero()
    {
        var wallet = TestData.Wallet(50m);

        BalanceAdjustment.Apply(wallet, AdminId, -50m, "Correction", "admin-adjust:1", Now);

        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public void ZeroAmount_IsRefused()
    {
        var wallet = TestData.Wallet(50m);

        Assert.Throws<DomainException>(() => BalanceAdjustment.Apply(wallet, AdminId, 0m, "Correction", "admin-adjust:1", Now));
        Assert.Empty(wallet.Transactions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("abcd")]
    [InlineData("  ab  ")]
    public void AMissingOrTooShortReason_IsRefused(string reason)
    {
        var wallet = TestData.Wallet(50m);

        Assert.Throws<DomainException>(() => BalanceAdjustment.Apply(wallet, AdminId, 10m, reason, "admin-adjust:1", Now));
        Assert.Equal(50m, wallet.Balance);
    }

    [Fact]
    public void AReasonLongerThan500Characters_IsRefused()
    {
        var wallet = TestData.Wallet(50m);

        Assert.Throws<DomainException>(() =>
            BalanceAdjustment.Apply(wallet, AdminId, 10m, new string('x', 501), "admin-adjust:1", Now));
    }

    [Fact]
    public void TheReason_IsStoredTrimmed()
    {
        var wallet = TestData.Wallet(50m);

        var adjustment = BalanceAdjustment.Apply(wallet, AdminId, 10m, "   Event prize   ", "admin-adjust:1", Now);

        Assert.Equal("Event prize", adjustment.Reason);
        Assert.Equal("Event prize", adjustment.WalletTransaction!.Reason);
    }

    [Fact]
    public void AnAdmin_CannotAdjustTheirOwnBalance()
    {
        var wallet = TestData.Wallet(50m); // belongs to user 1

        var error = Assert.Throws<DomainException>(() =>
            BalanceAdjustment.Apply(wallet, adminUserId: 1, 1000m, "Paying myself", "admin-adjust:1", Now));

        Assert.Contains("own balance", error.Message);
        Assert.Equal(50m, wallet.Balance);
    }

    [Fact]
    public void TheIdempotencyKey_IsKeptOnTheLedgerRow()
    {
        var wallet = TestData.Wallet(50m);

        var adjustment = BalanceAdjustment.Apply(wallet, AdminId, 10m, "Event prize", "admin-adjust:abc", Now);

        // The unique index on this key is what stops a double-click from paying twice
        Assert.Equal("admin-adjust:abc", adjustment.WalletTransaction!.IdempotencyKey);
    }
}
