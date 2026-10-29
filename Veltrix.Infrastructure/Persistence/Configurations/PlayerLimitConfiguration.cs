using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class PlayerLimitConfiguration : IEntityTypeConfiguration<PlayerLimit>
{
    public void Configure(EntityTypeBuilder<PlayerLimit> builder)
    {
        builder.ToTable("PlayerLimits", t =>
        {
            t.HasCheckConstraint("CK_PlayerLimits_DailyBetLimit", "[DailyBetLimit] IS NULL OR [DailyBetLimit] > 0");
            t.HasCheckConstraint("CK_PlayerLimits_PendingDailyBetLimit", "[PendingDailyBetLimit] IS NULL OR [PendingDailyBetLimit] > 0");
            // A waiting value without a date would never apply
            t.HasCheckConstraint("CK_PlayerLimits_PendingHasDate", "[PendingDailyBetLimit] IS NULL OR [PendingLimitAppliesAt] IS NOT NULL");
            t.HasCheckConstraint("CK_PlayerLimits_SessionReminder", "[SessionReminderMin] BETWEEN 0 AND 600");
        });

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
