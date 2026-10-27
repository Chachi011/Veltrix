namespace Veltrix.Domain.Crash;

/// <summary>
/// Groups multipliers into ranges for charts: crash points (admin dashboard) and cash-outs (player stats).
/// Also gives the share each range SHOULD have according to the provably fair formula, so the dashboard
/// can show "observed vs expected": if the server cheated, the two would drift apart over many rounds.
/// </summary>
public static class MultiplierBuckets
{
    /// <summary>Lower bounds. Bucket i is [Bounds[i], Bounds[i + 1]); the last one is open-ended.</summary>
    public static readonly IReadOnlyList<decimal> Bounds = new[] { 1m, 1.5m, 2m, 5m, 10m, 100m };

    public static int Count => Bounds.Count;

    public static int IndexOf(decimal multiplier)
    {
        for (var i = Bounds.Count - 1; i > 0; i--)
        {
            if (multiplier >= Bounds[i])
                return i;
        }

        return 0;
    }

    public static decimal? UpperBound(int index) => index + 1 < Bounds.Count ? Bounds[index + 1] : null;

    /// <summary>"1.00x - 1.49x" ... "100x+".</summary>
    public static string Label(int index) => UpperBound(index) is { } upper
        ? $"{Bounds[index]:0.00}x - {upper - 0.01m:0.00}x"
        : $"{Bounds[index]:0}x+";

    /// <summary>
    /// Theoretical probability that a round's crash point falls into the bucket.
    /// From the formula: P(crash >= x) = 0.97 / x for x above 1, and 1 for x = 1 (nothing crashes below 1.00x).
    /// </summary>
    public static double ExpectedShare(int index)
    {
        var lower = AtLeast((double)Bounds[index]);
        var upper = UpperBound(index) is { } u ? AtLeast((double)u) : 0d;
        return lower - upper;
    }

    /// <summary>How many values fall into each bucket (array index = bucket index).</summary>
    public static int[] Distribute(IEnumerable<decimal> multipliers)
    {
        var counts = new int[Bounds.Count];
        foreach (var multiplier in multipliers)
            counts[IndexOf(multiplier)]++;

        return counts;
    }

    private static double AtLeast(double x) => x <= 1 ? 1d : (1 - ProvablyFair.HouseEdge) / x;
}
