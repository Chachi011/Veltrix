using Veltrix.Application.DTOs.Stats;
using Veltrix.Domain.Crash;

namespace Veltrix.Infrastructure.Services;

internal static class BucketMapping
{
    /// <summary>Turns bucket counts into chart rows with the observed (and optionally expected) share in percent.</summary>
    public static IReadOnlyList<MultiplierBucketDto> ToDtos(int[] counts, bool includeExpected)
    {
        var total = counts.Sum();

        return counts
            .Select((count, i) => new MultiplierBucketDto
            {
                Label = MultiplierBuckets.Label(i),
                From = MultiplierBuckets.Bounds[i],
                To = MultiplierBuckets.UpperBound(i),
                Count = count,
                Percent = total == 0 ? 0 : Math.Round(count * 100m / total, 2),
                ExpectedPercent = includeExpected ? Math.Round((decimal)MultiplierBuckets.ExpectedShare(i) * 100m, 2) : null
            })
            .ToList();
    }
}
