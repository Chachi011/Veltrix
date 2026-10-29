using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class SelfExclusionConfiguration : IEntityTypeConfiguration<SelfExclusion>
{
    public void Configure(EntityTypeBuilder<SelfExclusion> builder)
    {
        builder.ToTable("SelfExclusions", t =>
            t.HasCheckConstraint("CK_SelfExclusions_Period", "[EndsAt] > [StartsAt]"));

        builder.HasIndex(x => new { x.UserId, x.EndsAt });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
