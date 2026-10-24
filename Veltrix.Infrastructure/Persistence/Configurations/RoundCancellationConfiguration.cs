using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class RoundCancellationConfiguration : IEntityTypeConfiguration<RoundCancellation>
{
    public void Configure(EntityTypeBuilder<RoundCancellation> builder)
    {
        builder.ToTable("RoundCancellations", t =>
            t.HasCheckConstraint("CK_RoundCancellations_Refunds", "[RefundedBetsCount] >= 0 AND [RefundedTotal] >= 0"));

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        // A round can be cancelled only once
        builder.HasIndex(x => x.RoundId).IsUnique();

        builder.HasOne(x => x.Round)
            .WithMany()
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
