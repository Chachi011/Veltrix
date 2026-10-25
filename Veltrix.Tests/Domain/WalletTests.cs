using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

public class WalletTests
{
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void Debit_ReducesBalance_AndRecordsBeforeAndAfter()
    {
        var wallet = TestData.Wallet(100m);

        var tx = wallet.Debit(30m, TransactionType.Bet, "bet:1", Now);

        Assert.Equal(70m, wallet.Balance);
        Assert.Equal(-30m, tx.Amount);
        Assert.Equal(100m, tx.BalanceBefore);
        Assert.Equal(70m, tx.BalanceAfter);
        Assert.Equal("bet:1", tx.IdempotencyKey);
        Assert.Single(wallet.Transactions);
    }

    [Fact]
    public void Debit_MoreThanTheBalance_Throws_AndChangesNothing()
    {
        var wallet = TestData.Wallet(50m);

        Assert.Throws<InsufficientBalanceException>(() => wallet.Debit(50.01m, TransactionType.Bet, "bet:1", Now));

        Assert.Equal(50m, wallet.Balance);
        Assert.Empty(wallet.Transactions);
    }

    [Fact]
    public void Debit_TheWholeBalance_LeavesExactlyZero()
    {
        var wallet = TestData.Wallet(50m);

        wallet.Debit(50m, TransactionType.Bet, "bet:1", Now);

        Assert.Equal(0m, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ZeroOrNegativeAmounts_AreRejected(double amount)
    {
        var wallet = TestData.Wallet(100m);

        Assert.Throws<DomainException>(() => wallet.Debit((decimal)amount, TransactionType.Bet, "k1", Now));
        Assert.Throws<DomainException>(() => wallet.Credit((decimal)amount, TransactionType.Win, "k2", Now));
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Refund_AfterBet_RestoresTheBalance_WithTwoLedgerRows()
    {
        var wallet = TestData.Wallet(100m);

        wallet.Debit(40m, TransactionType.Bet, "bet:1", Now);
        wallet.Credit(40m, TransactionType.Refund, "refund:1", Now);

        Assert.Equal(100m, wallet.Balance);
        Assert.Equal(new[] { TransactionType.Bet, TransactionType.Refund }, wallet.Transactions.Select(t => t.Type));
        Assert.All(wallet.Transactions, t => Assert.Equal(t.BalanceBefore + t.Amount, t.BalanceAfter));
    }

    [Fact]
    public void DailyBonus_CanBeClaimedOncePer24Hours()
    {
        var wallet = TestData.Wallet(0m);

        wallet.ClaimDailyBonus(100m, Now);
        Assert.Equal(100m, wallet.Balance);
        Assert.False(wallet.CanClaimDailyBonus(Now.AddHours(23).AddMinutes(59)));

        Assert.Throws<DomainException>(() => wallet.ClaimDailyBonus(100m, Now.AddHours(23)));
        Assert.Equal(100m, wallet.Balance);

        wallet.ClaimDailyBonus(100m, Now.AddHours(24));
        Assert.Equal(200m, wallet.Balance);
    }

    [Fact]
    public void DailyBonus_EachClaimHasADifferentIdempotencyKey()
    {
        var wallet = TestData.Wallet(0m);

        var first = wallet.ClaimDailyBonus(100m, Now);
        var second = wallet.ClaimDailyBonus(100m, Now.AddDays(1));

        Assert.NotEqual(first.IdempotencyKey, second.IdempotencyKey);
    }
}
