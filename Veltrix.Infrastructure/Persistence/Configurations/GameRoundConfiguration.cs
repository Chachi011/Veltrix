using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable("GameRounds", t =>
            t.HasCheckConstraint("CK_GameRounds_CrashPoint", "[CrashPoint] >= 1"));

        builder.Property(x => x.CrashPoint).HasPrecision(10, 2);
        builder.Property(x => x.ServerSeed).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ServerSeedHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.RoundNumber }).IsUnique();
        builder.HasIndex(x => x.State);

        builder.HasOne(x => x.Game)
            .WithMany(x => x.Rounds)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
