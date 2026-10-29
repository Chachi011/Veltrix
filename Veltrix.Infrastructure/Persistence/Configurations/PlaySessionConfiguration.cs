using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class PlaySessionConfiguration : IEntityTypeConfiguration<PlaySession>
{
    public void Configure(EntityTypeBuilder<PlaySession> builder)
    {
        builder.ToTable("PlaySessions", t =>
            t.HasCheckConstraint("CK_PlaySessions_Activity", "[LastActivityAt] >= [StartedAt]"));

        builder.HasIndex(x => new { x.UserId, x.StartedAt });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
