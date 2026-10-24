using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions", t =>
        {
            t.HasCheckConstraint("CK_WalletTransactions_BalanceAfter_NonNegative", "[BalanceAfter] >= 0");
            // The ledger must always add up
            t.HasCheckConstraint("CK_WalletTransactions_Balance_Math", "[BalanceAfter] = [BalanceBefore] + [Amount]");
        });

        builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        // Same operation can never be stored twice
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();

        // Transaction history is always "my wallet, newest first"
        builder.HasIndex(x => new { x.WalletId, x.CreatedAt });

        builder.HasOne(x => x.Wallet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.BetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
