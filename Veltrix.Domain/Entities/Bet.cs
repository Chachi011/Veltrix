using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class Bet
{
    public const decimal MinAutoCashOut = 1.01m;
    public const int MaxSlot = 2;

    public int Id { get; set; }
    public int RoundId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>1 or 2 (slot 2 is the optional "two bets per round" feature).</summary>
    public int Slot { get; set; } = 1;

    public decimal? AutoCashOutAt { get; set; }
    public decimal? CashOutMultiplier { get; set; }
    public decimal? Payout { get; set; }

    /// <summary>
    /// Also a concurrency token: an UPDATE only succeeds if the status is still what we read.
    /// So a cash-out and the crash settlement can never both change the same bet.
    /// </summary>
    public BetStatus Status { get; set; } = BetStatus.Active;

    public DateTime PlacedAt { get; set; }
    public DateTime? CashedOutAt { get; set; }

    public GameRound? Round { get; set; }
    public User? User { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    /// <summary>Creates a bet after checking every rule that does not need the database.</summary>
    public static Bet Place(GameRound round, Game game, int userId, decimal amount, int slot, decimal? autoCashOutAt, DateTime now)
    {
        if (!round.AcceptsBets(now))
            throw new DomainException("Betting is closed for this round.");

        if (amount < game.MinBet || amount > game.MaxBet)
            throw new DomainException($"Bet must be between {game.MinBet:0.00} and {game.MaxBet:0.00} VLX.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Bet amount can have at most 2 decimal places.");

        if (slot < 1 || slot > MaxSlot)
            throw new DomainException($"Slot must be between 1 and {MaxSlot}.");

        if (autoCashOutAt is < MinAutoCashOut)
            throw new DomainException($"Auto cash-out must be at least {MinAutoCashOut:0.00}x.");

        return new Bet
        {
            RoundId = round.Id,
            UserId = userId,
            Amount = amount,
            Slot = slot,
            AutoCashOutAt = autoCashOutAt,
            Status = BetStatus.Active,
            PlacedAt = now
        };
    }

    /// <summary>
    /// Manual cash-out: uses the SERVER's multiplier at <paramref name="now"/>, never a value from the client.
    /// </summary>
    /// <returns>The payout.</returns>
    public decimal CashOut(GameRound round, DateTime now)
    {
        if (round.State != RoundState.Running)
            throw new DomainException("You can cash out only while the round is running.");

        var multiplier = round.MultiplierAt(now);
        if (multiplier >= round.CrashPoint)
            throw new DomainException("Too late: the round has already crashed.");

        return CashOutAt(multiplier, now);
    }

    /// <summary>Cash-out at a known multiplier (used for auto cash-out).</summary>
    public decimal CashOutAt(decimal multiplier, DateTime now)
    {
        EnsureActive();

        if (multiplier < 1)
            throw new DomainException("Multiplier cannot be below 1.00x.");

        // Rounded DOWN to the cent, so the house never pays a fraction more than earned
        var payout = Math.Round(Amount * multiplier, 2, MidpointRounding.ToZero);

        Status = BetStatus.CashedOut;
        CashOutMultiplier = multiplier;
        Payout = payout;
        CashedOutAt = now;
        return payout;
    }

    public void MarkLost()
    {
        EnsureActive();
        Status = BetStatus.Lost;
        Payout = 0;
    }

    public void MarkRefunded()
    {
        EnsureActive();
        Status = BetStatus.Refunded;
        Payout = Amount;
    }

    private void EnsureActive()
    {
        if (Status != BetStatus.Active)
            throw new DomainException($"This bet is already {Status}.");
    }
}
