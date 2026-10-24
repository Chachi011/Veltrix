namespace Veltrix.Domain.Crash;

/// <summary>
/// How the multiplier grows with time: m(t) = e^(k·t), t in milliseconds.
/// Exponential growth starts slowly (good for reaction time) and speeds up:
/// 2x after ~11.6 s, 10x after ~38 s, 100x after ~77 s.
/// Server and browser use the same formula, so the client animation matches the server.
/// </summary>
public static class MultiplierCurve
{
    public const double GrowthPerMs = 0.00006;

    /// <summary>Multiplier at the given time since the round started, rounded DOWN to 2 decimals.</summary>
    public static decimal At(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return 1.00m;

        var value = Math.Exp(GrowthPerMs * elapsed.TotalMilliseconds);
        if (value >= (double)ProvablyFair.MaxCrashPoint)
            return ProvablyFair.MaxCrashPoint;

        return Math.Floor((decimal)value * 100) / 100;
    }

    /// <summary>Inverse: how long after the start the multiplier reaches the given value.</summary>
    public static TimeSpan TimeToReach(decimal multiplier) =>
        multiplier <= 1
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(Math.Log((double)multiplier) / GrowthPerMs);
}
