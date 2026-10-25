using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Veltrix.Domain.Crash;

/// <summary>
/// Provably fair crash points.
///
/// 1. Before the round the server creates a random secret <c>serverSeed</c>
///    and publishes only <c>SHA-256(serverSeed)</c>.
/// 2. The crash point is calculated from the seed alone, so it is fixed before anyone bets.
/// 3. After the crash the seed is revealed. Anyone can check that
///    SHA-256(seed) equals the published hash AND that the seed gives the same crash point.
///    If the server had changed the result, one of the two checks would fail.
/// </summary>
public static class ProvablyFair
{
    /// <summary>3% house edge: about 3% of rounds crash instantly at 1.00x.</summary>
    public const double HouseEdge = 0.03;

    /// <summary>Public, constant HMAC message. Part of the published algorithm.</summary>
    public const string Salt = "VELTRIX";

    public const decimal MaxCrashPoint = 1_000_000m;

    public const string AlgorithmDescription =
        "hash = SHA256(serverSeed). " +
        "h = first 52 bits of HMAC_SHA256(key = serverSeed, message = \"VELTRIX\"); r = h / 2^52. " +
        "crashPoint = max(1.00, floor(100 * 0.97 / (1 - r)) / 100), capped at 1,000,000.";

    private const int SeedBytes = 32;
    private const double TwoPow52 = 4503599627370496d; // 2^52

    /// <summary>64 hex chars from a cryptographically secure generator (unpredictable).</summary>
    public static string GenerateServerSeed() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(SeedBytes)).ToLowerInvariant();

    public static string HashSeed(string serverSeed) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serverSeed))).ToLowerInvariant();

    public static decimal CalculateCrashPoint(string serverSeed)
    {
        var hmac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(serverSeed), Encoding.UTF8.GetBytes(Salt));

        // First 13 hex characters = 52 bits = exactly representable in a double
        var hex = Convert.ToHexString(hmac)[..13];
        var h = ulong.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var r = h / TwoPow52; // uniform in [0, 1)

        // P(crash >= x) = 0.97 / x, so every cash-out target returns 97% on average:
        // there is no strategy that beats the house edge.
        var raw = (1 - HouseEdge) / (1 - r);
        var crash = Math.Floor(raw * 100) / 100;

        if (crash < 1)
            return 1.00m;

        return crash >= (double)MaxCrashPoint ? MaxCrashPoint : Math.Round((decimal)crash, 2);
    }

    public static bool Verify(string serverSeed, string serverSeedHash, decimal crashPoint) =>
        string.Equals(HashSeed(serverSeed), serverSeedHash, StringComparison.OrdinalIgnoreCase)
        && CalculateCrashPoint(serverSeed) == crashPoint;
}
