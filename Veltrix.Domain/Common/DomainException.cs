using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Common;

/// <summary>
/// A business rule was broken (e.g. "betting is closed", "insufficient balance").
/// Thrown by Domain entities; the API turns it into a 400 ProblemDetails.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>The round state machine was asked to do an illegal jump (e.g. Waiting → Running).</summary>
public sealed class InvalidRoundTransitionException : DomainException
{
    public InvalidRoundTransitionException(RoundState from, RoundState to)
        : base($"A round cannot go from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    public RoundState From { get; }
    public RoundState To { get; }
}

public sealed class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException(decimal balance, decimal requested)
        : base($"Insufficient balance: you have {balance:0.00} VLX, but {requested:0.00} VLX is needed.") { }
}

/// <summary>The player paused their own betting (responsible play). The API answers 403.</summary>
public sealed class SelfExcludedException : DomainException
{
    public SelfExcludedException(DateTime until)
        : base($"You are self-excluded until {until:yyyy-MM-dd HH:mm} UTC.")
    {
        Until = until;
    }

    public DateTime Until { get; }
}

public sealed class DailyBetLimitExceededException : DomainException
{
    public DailyBetLimitExceededException(decimal betToday, decimal limit)
        : base($"Daily bet limit reached: {betToday:0.00} of {limit:0.00} VLX used today.") { }
}
