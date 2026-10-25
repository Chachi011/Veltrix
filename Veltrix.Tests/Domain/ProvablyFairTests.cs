using Veltrix.Domain.Crash;

namespace Veltrix.Tests.Domain;

public class ProvablyFairTests
{
    [Fact]
    public void HashSeed_IsStandardSha256_InLowercaseHex()
    {
        // Official SHA-256 test vector for "abc": anyone can recompute it with any SHA-256 tool
        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            ProvablyFair.HashSeed("abc"));
    }

    [Fact]
    public void GenerateServerSeed_Returns64HexChars_AndIsDifferentEachTime()
    {
        var first = ProvablyFair.GenerateServerSeed();
        var second = ProvablyFair.GenerateServerSeed();

        Assert.Matches("^[0-9a-f]{64}$", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CalculateCrashPoint_IsDeterministic_SameSeedSameResult()
    {
        var seed = ProvablyFair.GenerateServerSeed();

        Assert.Equal(ProvablyFair.CalculateCrashPoint(seed), ProvablyFair.CalculateCrashPoint(seed));
    }

    [Fact]
    public void CalculateCrashPoint_IsAlwaysAtLeastOne_WithTwoDecimals()
    {
        for (var i = 0; i < 5_000; i++)
        {
            var crash = ProvablyFair.CalculateCrashPoint($"seed-{i}");

            Assert.InRange(crash, 1.00m, ProvablyFair.MaxCrashPoint);
            Assert.Equal(crash, decimal.Round(crash, 2));
        }
    }

    [Fact]
    public void CrashPointDistribution_MatchesHouseEdgeFormula()
    {
        // Fixed seeds -> the test always gives the same result (no flaky randomness)
        const int rounds = 20_000;
        var crashes = Enumerable.Range(0, rounds).Select(i => ProvablyFair.CalculateCrashPoint($"distribution-{i}")).ToList();

        var instant = crashes.Count(c => c == 1.00m) / (double)rounds;
        var atLeast2X = crashes.Count(c => c >= 2.00m) / (double)rounds;
        var atLeast10X = crashes.Count(c => c >= 10.00m) / (double)rounds;

        // Theory: P(crash >= x) = 0.97 / x, so ~3% instant crashes, 48.5% reach 2x, 9.7% reach 10x
        Assert.InRange(instant, 0.02, 0.045);
        Assert.InRange(atLeast2X, 0.465, 0.505);
        Assert.InRange(atLeast10X, 0.085, 0.11);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForTheRealSeedHashAndCrashPoint()
    {
        var seed = ProvablyFair.GenerateServerSeed();
        var hash = ProvablyFair.HashSeed(seed);
        var crash = ProvablyFair.CalculateCrashPoint(seed);

        Assert.True(ProvablyFair.Verify(seed, hash, crash));
        Assert.True(ProvablyFair.Verify(seed, hash.ToUpperInvariant(), crash));
    }

    [Fact]
    public void Verify_ReturnsFalse_IfTheServerChangedTheCrashPoint()
    {
        var seed = ProvablyFair.GenerateServerSeed();
        var hash = ProvablyFair.HashSeed(seed);
        var faked = ProvablyFair.CalculateCrashPoint(seed) + 0.01m;

        Assert.False(ProvablyFair.Verify(seed, hash, faked));
    }

    [Fact]
    public void Verify_ReturnsFalse_IfTheRevealedSeedDoesNotMatchThePublishedHash()
    {
        var publishedHash = ProvablyFair.HashSeed(ProvablyFair.GenerateServerSeed());
        var otherSeed = ProvablyFair.GenerateServerSeed();

        Assert.False(ProvablyFair.Verify(otherSeed, publishedHash, ProvablyFair.CalculateCrashPoint(otherSeed)));
    }
}
