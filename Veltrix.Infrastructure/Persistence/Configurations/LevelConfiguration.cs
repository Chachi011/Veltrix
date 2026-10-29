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

        builder.HasData(
            new Level { Number = 1, Title = "Rookie", XpRequired = 0 },
            new Level { Number = 2, Title = "Spotter", XpRequired = 100 },
            new Level { Number = 3, Title = "Climber", XpRequired = 250 },
            new Level { Number = 4, Title = "Pilot", XpRequired = 500 },
            new Level { Number = 5, Title = "Navigator", XpRequired = 1_000 },
            new Level { Number = 6, Title = "Ace", XpRequired = 2_000 },
            new Level { Number = 7, Title = "Veteran", XpRequired = 3_500 },
            new Level { Number = 8, Title = "Elite", XpRequired = 5_500 },
            new Level { Number = 9, Title = "Legend", XpRequired = 8_000 },
            new Level { Number = 10, Title = "Mythic", XpRequired = 12_000 });
    }
}
