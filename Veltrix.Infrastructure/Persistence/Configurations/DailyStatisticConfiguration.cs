using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class DailyStatisticConfiguration : IEntityTypeConfiguration<DailyStatistic>
{
    public void Configure(EntityTypeBuilder<DailyStatistic> builder)
    {
        builder.ToTable("DailyStatistics", t =>
            t.HasCheckConstraint("CK_DailyStatistics_NonNegative",
                "[ActiveUsers] >= 0 AND [NewUsers] >= 0 AND [RoundsPlayed] >= 0 AND [BetsCount] >= 0 AND [BetsVolume] >= 0 AND [PayoutVolume] >= 0"));

        // Natural key: one row per calendar day
        builder.HasKey(x => x.Date);
        builder.Property(x => x.AverageCrashPoint).HasPrecision(10, 2);
    }
}
