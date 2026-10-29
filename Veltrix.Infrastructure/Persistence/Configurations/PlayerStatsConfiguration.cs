using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class PlayerStatsConfiguration : IEntityTypeConfiguration<PlayerStats>
{
    public void Configure(EntityTypeBuilder<PlayerStats> builder)
    {
        builder.ToTable("PlayerStats", t =>
        {
            t.HasCheckConstraint("CK_PlayerStats_NonNegative",
                "[TotalBets] >= 0 AND [CashOutCount] >= 0 AND [TotalWagered] >= 0 AND [TotalPayout] >= 0 AND [Xp] >= 0");
            t.HasCheckConstraint("CK_PlayerStats_CashOuts", "[CashOutCount] <= [TotalBets]");
        });

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.BiggestMultiplier).HasPrecision(10, 2);

        // Leaderboard "all time": ORDER BY these columns
        builder.HasIndex(x => x.BiggestMultiplier);
        builder.HasIndex(x => x.BiggestWin);

        builder.HasOne(x => x.User)
            .WithOne(x => x.Stats)
            .HasForeignKey<PlayerStats>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Level)
            .WithMany()
            .HasForeignKey(x => x.LevelNumber)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
