using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class UserTwoFactorConfiguration : IEntityTypeConfiguration<UserTwoFactor>
{
    public void Configure(EntityTypeBuilder<UserTwoFactor> builder)
    {
        builder.ToTable("UserTwoFactors");

        // Shared primary key: UserId is both PK and FK, which makes "at most one row per user" impossible to break
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.SecretKey).HasMaxLength(200).IsRequired();

        builder.HasOne(x => x.User)
            .WithOne(x => x.TwoFactor)
            .HasForeignKey<UserTwoFactor>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
