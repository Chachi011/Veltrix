using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements", t =>
            t.HasCheckConstraint("CK_Achievements_XpReward", "[XpReward] >= 0"));

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new Achievement { Id = 1, Code = "FIRST_BET", Title = "First bet", Description = "Place your first bet.", Icon = "rocket", XpReward = 10 },
            new Achievement { Id = 2, Code = "FIRST_CASHOUT", Title = "First cash-out", Description = "Cash out before the crash for the first time.", Icon = "coins", XpReward = 20 },
            new Achievement { Id = 3, Code = "CASHOUT_10X", Title = "To the moon", Description = "Cash out at 10.00x or higher.", Icon = "bolt", XpReward = 100 },
            new Achievement { Id = 4, Code = "BETS_100", Title = "Regular", Description = "Place 100 bets.", Icon = "trophy", XpReward = 50 },
            new Achievement { Id = 5, Code = "BIG_WIN_1000", Title = "Big win", Description = "Win 1000 VLX or more in a single bet.", Icon = "star", XpReward = 100 });
    }
}
