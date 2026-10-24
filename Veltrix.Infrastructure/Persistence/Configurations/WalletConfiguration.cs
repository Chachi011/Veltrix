using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        // Last line of defence: even if application code has a bug, SQL Server refuses a negative balance
        builder.ToTable("Wallets", t =>
            t.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "[Balance] >= 0"));

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
