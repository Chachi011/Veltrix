using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences", t =>
            t.HasCheckConstraint("CK_UserPreferences_Language", "[Language] IN ('en', 'ka')"));

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.Language).HasMaxLength(5).IsRequired();

        builder.HasOne(x => x.User)
            .WithOne(x => x.Preference)
            .HasForeignKey<UserPreference>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
