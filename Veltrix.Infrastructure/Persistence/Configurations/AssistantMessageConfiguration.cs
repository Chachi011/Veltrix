using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("AssistantMessages");

        builder.Property(x => x.Text).HasMaxLength(AssistantMessage.MaxTextLength).IsRequired();

        // The chat panel asks "the latest messages of user X"
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        // Deleting a user deletes their conversation; the User entity needs no collection for it
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
