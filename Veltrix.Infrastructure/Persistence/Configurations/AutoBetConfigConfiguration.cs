using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class AutoBetConfigConfiguration : IEntityTypeConfiguration<AutoBetConfig>
{
    public void Configure(EntityTypeBuilder<AutoBetConfig> builder)
    {
        builder.ToTable("AutoBetConfigs", t =>
        {
            t.HasCheckConstraint("CK_AutoBetConfigs_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_AutoBetConfigs_CashOut", "[AutoCashOutAt] >= 1.01");
            t.HasCheckConstraint("CK_AutoBetConfigs_Rounds", "[TotalRounds] BETWEEN 1 AND 1000 AND [RoundsPlayed] BETWEEN 0 AND [TotalRounds]");
        });

        builder.Property(x => x.AutoCashOutAt).HasPrecision(10, 2);

        // At most ONE active auto-bet per player per game (filtered unique index)
        builder.HasIndex(x => new { x.UserId, x.GameId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
