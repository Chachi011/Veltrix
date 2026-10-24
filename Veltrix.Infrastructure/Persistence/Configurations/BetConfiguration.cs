using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("Bets", t =>
        {
            t.HasCheckConstraint("CK_Bets_Amount_Positive", "[Amount] > 0");
            t.HasCheckConstraint("CK_Bets_Slot", "[Slot] IN (1, 2)");
            t.HasCheckConstraint("CK_Bets_AutoCashOut", "[AutoCashOutAt] IS NULL OR [AutoCashOutAt] >= 1.01");
        });

        builder.Property(x => x.AutoCashOutAt).HasPrecision(10, 2);
        builder.Property(x => x.CashOutMultiplier).HasPrecision(10, 2);

        // UPDATE ... WHERE Status = <value we read>: a cash-out and the crash settlement
        // can never both change the same bet (the second one gets a concurrency error)
        builder.Property(x => x.Status).IsConcurrencyToken();

        // One bet per slot per player per round (prevents double-click double bets)
        builder.HasIndex(x => new { x.RoundId, x.UserId, x.Slot }).IsUnique();

        // "My bets" history page
        builder.HasIndex(x => new { x.UserId, x.PlacedAt });

        builder.HasOne(x => x.Round)
            .WithMany(x => x.Bets)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Bets)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
