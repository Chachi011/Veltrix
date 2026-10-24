using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Persistence.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(100);
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();

        builder.HasData(
            new SystemSetting { Key = SettingKeys.StartingBalance, Value = "1000", UpdatedAt = SeedData.CreatedAt },
            new SystemSetting { Key = SettingKeys.DailyBonusAmount, Value = "100", UpdatedAt = SeedData.CreatedAt });
    }
}
