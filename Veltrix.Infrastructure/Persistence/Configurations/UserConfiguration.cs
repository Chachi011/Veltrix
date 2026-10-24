using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Nickname).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Nickname).IsUnique();

        builder.HasOne(x => x.Wallet)
            .WithOne(x => x.User)
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Limit)
            .WithOne(x => x.User)
            .HasForeignKey<PlayerLimit>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
