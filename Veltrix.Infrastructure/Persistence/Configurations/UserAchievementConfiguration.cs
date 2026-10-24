using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");

        // Composite key: the same achievement can be unlocked only once per user
        builder.HasKey(x => new { x.UserId, x.AchievementId });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Achievement)
            .WithMany(x => x.UnlockedBy)
            .HasForeignKey(x => x.AchievementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
