using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class BalanceAdjustmentConfiguration : IEntityTypeConfiguration<BalanceAdjustment>
{
    public void Configure(EntityTypeBuilder<BalanceAdjustment> builder)
    {
        builder.ToTable("BalanceAdjustments", t =>
        {
            t.HasCheckConstraint("CK_BalanceAdjustments_Amount", "[Amount] <> 0");
            t.HasCheckConstraint("CK_BalanceAdjustments_Reason", "LEN([Reason]) >= 5");
        });

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        // Exactly one ledger entry per adjustment
        builder.HasIndex(x => x.WalletTransactionId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        // Two relationships to the same Users table (player and admin), so both must be named explicitly
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AdminUser)
            .WithMany()
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WalletTransaction)
            .WithOne()
            .HasForeignKey<BalanceAdjustment>(x => x.WalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
