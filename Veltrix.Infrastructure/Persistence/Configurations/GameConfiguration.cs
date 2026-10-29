using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games", t =>
        {
            t.HasCheckConstraint("CK_Games_MinBet_Positive", "[MinBet] > 0");
            t.HasCheckConstraint("CK_Games_MaxBet_GteMinBet", "[MaxBet] >= [MinBet]");
            t.HasCheckConstraint("CK_Games_BettingDuration", "[BettingDurationSec] BETWEEN 3 AND 120");
            t.HasCheckConstraint("CK_Games_Pause", "[PauseBetweenRoundsSec] BETWEEN 0 AND 120");
        });

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => x.Slug).IsUnique();

        // Reference data: created by the migration itself, so every fresh DB has a playable game
        builder.HasData(new Game
        {
            Id = 1,
            Name = "Crash",
            Slug = "crash",
            Description = "The multiplier grows from 1.00x. Cash out before it crashes!",
            Type = GameType.Crash,
            Status = GameStatus.Active,
            MinBet = 1m,
            MaxBet = 10_000m,
            BettingDurationSec = 10,
            PauseBetweenRoundsSec = 5,
            CreatedAt = SeedData.CreatedAt
        });
    }
}
