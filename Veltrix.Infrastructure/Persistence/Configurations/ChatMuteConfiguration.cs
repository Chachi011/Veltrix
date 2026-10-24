using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class ChatMuteConfiguration : IEntityTypeConfiguration<ChatMute>
{
    public void Configure(EntityTypeBuilder<ChatMute> builder)
    {
        builder.ToTable("ChatMutes", t =>
            t.HasCheckConstraint("CK_ChatMutes_Until", "[MutedUntil] > [CreatedAt]"));

        builder.Property(x => x.Reason).HasMaxLength(300);

        // "Is user X muted right now?"
        builder.HasIndex(x => new { x.UserId, x.MutedUntil });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MutedByUser)
            .WithMany()
            .HasForeignKey(x => x.MutedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
