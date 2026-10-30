using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> builder)
    {
        builder.ToTable("Levels", t =>
            t.HasCheckConstraint("CK_Levels_Xp", "[XpRequired] >= 0"));

        builder.HasKey(x => x.Number);
        // We choose the level numbers ourselves (1, 2, 3...), so no IDENTITY
        builder.Property(x => x.Number).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.XpRequired).IsUnique();

        // XP comes only from achievements (200 XP in total), so completing every task, mostly learning and safety,
        // reaches the top level; playing alone (50 XP) reaches level 4 at most
        builder.HasData(
            new Level { Number = 1, Title = "Rookie", XpRequired = 0 },
            new Level { Number = 2, Title = "Spotter", XpRequired = 10 },
            new Level { Number = 3, Title = "Climber", XpRequired = 30 },
            new Level { Number = 4, Title = "Pilot", XpRequired = 50 },
            new Level { Number = 5, Title = "Navigator", XpRequired = 70 },
            new Level { Number = 6, Title = "Ace", XpRequired = 90 },
            new Level { Number = 7, Title = "Veteran", XpRequired = 110 },
            new Level { Number = 8, Title = "Elite", XpRequired = 140 },
            new Level { Number = 9, Title = "Legend", XpRequired = 170 },
            new Level { Number = 10, Title = "Mythic", XpRequired = 200 });
    }
}
