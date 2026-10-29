using Veltrix.Domain.Crash;

namespace Veltrix.Tests.Domain;

public class MultiplierCurveTests
{
    [Fact]
    public void At_StartsAtOne()
    {
        Assert.Equal(1.00m, MultiplierCurve.At(TimeSpan.Zero));
        Assert.Equal(1.00m, MultiplierCurve.At(TimeSpan.FromMilliseconds(-50)));
    }

    [Fact]
    public void At_GrowsExponentially()
    {
        // e^(0.00006 * 11552) ≈ 2.00
        Assert.InRange(MultiplierCurve.At(TimeSpan.FromSeconds(11.6)), 2.00m, 2.01m);
        Assert.InRange(MultiplierCurve.At(TimeSpan.FromSeconds(38.4)), 9.99m, 10.02m);
    }

    [Fact]
    public void At_NeverDecreases()
    {
        var previous = 1.00m;
        for (var ms = 0; ms <= 60_000; ms += 100)
        {
            var current = MultiplierCurve.At(TimeSpan.FromMilliseconds(ms));
            Assert.True(current >= previous, $"Multiplier dropped at {ms} ms");
            previous = current;
        }
    }

    [Theory]
    [InlineData(1.01)]
    [InlineData(2.00)]
    [InlineData(13.37)]
    [InlineData(250.00)]
    public void TimeToReach_IsTheInverseOfAt(double target)
    {
        var multiplier = (decimal)target;
        var elapsed = MultiplierCurve.TimeToReach(multiplier);

        // Just before: still below the target; a moment later: reached
        Assert.True(MultiplierCurve.At(elapsed - TimeSpan.FromMilliseconds(5)) < multiplier);
        Assert.True(MultiplierCurve.At(elapsed + TimeSpan.FromMilliseconds(5)) >= multiplier);
    }
}
