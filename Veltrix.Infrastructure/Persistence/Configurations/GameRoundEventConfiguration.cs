using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class GameRoundEventConfiguration : IEntityTypeConfiguration<GameRoundEvent>
{
    public void Configure(EntityTypeBuilder<GameRoundEvent> builder)
    {
        builder.ToTable("GameRoundEvents");

        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.RoundId, x.OccurredAt });

        builder.HasOne(x => x.Round)
            .WithMany()
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
