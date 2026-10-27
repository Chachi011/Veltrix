using Veltrix.Domain.Crash;

namespace Veltrix.Tests.Domain;

/// <summary>Multiplier ranges for the charts, and the theory the admin dashboard compares against.</summary>
public class MultiplierBucketsTests
{
    [Theory]
    [InlineData(1.00, 0)]
    [InlineData(1.49, 0)]
    [InlineData(1.50, 1)]
    [InlineData(1.99, 1)]
    [InlineData(2.00, 2)]
    [InlineData(4.99, 2)]
    [InlineData(5.00, 3)]
    [InlineData(9.99, 3)]
    [InlineData(10.00, 4)]
    [InlineData(99.99, 4)]
    [InlineData(100.00, 5)]
    [InlineData(1000000.00, 5)]
    public void IndexOf_PutsEachValueInTheRightRange(double multiplier, int expected)
    {
        Assert.Equal(expected, MultiplierBuckets.IndexOf((decimal)multiplier));
    }

    [Fact]
    public void Labels_DescribeTheRanges()
    {
        Assert.Equal("1.00x - 1.49x", MultiplierBuckets.Label(0));
        Assert.Equal("10.00x - 99.99x", MultiplierBuckets.Label(4));
        Assert.Equal("100x+", MultiplierBuckets.Label(5));
    }

    [Fact]
    public void ExpectedShares_AddUpToOneHundredPercent()
    {
        var total = Enumerable.Range(0, MultiplierBuckets.Count).Sum(MultiplierBuckets.ExpectedShare);

        Assert.Equal(1.0, total, precision: 10);
    }

    [Fact]
    public void ExpectedShare_FollowsTheFormula()
    {
        // P(crash < 1.5) = 1 - 0.97 / 1.5 and P(crash >= 100) = 0.97 / 100
        Assert.Equal(1 - 0.97 / 1.5, MultiplierBuckets.ExpectedShare(0), precision: 10);
        Assert.Equal(0.97 / 100, MultiplierBuckets.ExpectedShare(5), precision: 10);
    }

    [Fact]
    public void Distribute_CountsEveryValueOnce()
    {
        var counts = MultiplierBuckets.Distribute(new[] { 1.00m, 1.20m, 1.80m, 3m, 7m, 50m, 250m, 1.49m });

        Assert.Equal(new[] { 3, 1, 1, 1, 1, 1 }, counts);
    }

    [Fact]
    public void RealCrashPoints_FollowTheExpectedDistribution()
    {
        // 20,000 rounds from the real generator (fixed seeds, so the test always gives the same result).
        // If the crash point formula were wrong, the observed shares would drift away from the theory.
        const int rounds = 20_000;
        var points = Enumerable.Range(0, rounds).Select(i => ProvablyFair.CalculateCrashPoint($"test-seed-{i}"));

        var counts = MultiplierBuckets.Distribute(points);

        for (var i = 0; i < MultiplierBuckets.Count; i++)
        {
            var observed = counts[i] / (double)rounds;
            Assert.InRange(observed, MultiplierBuckets.ExpectedShare(i) - 0.015, MultiplierBuckets.ExpectedShare(i) + 0.015);
        }
    }
}
