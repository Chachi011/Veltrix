using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        // 45 = longest textual IPv6 address
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.FailureReason).HasMaxLength(50);

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
