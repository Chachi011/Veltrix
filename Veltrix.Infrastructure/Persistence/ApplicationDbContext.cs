using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<PlayerLimit> PlayerLimits => Set<PlayerLimit>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Default for all money columns. Multipliers override this in their configurations.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        // Enums are stored as readable text ("Admin", "BettingOpen") instead of numbers
        configurationBuilder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<GameType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<GameStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<RoundState>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<BetStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TransactionType>().HaveConversion<string>().HaveMaxLength(20);

        // All times are stored in UTC. datetime2 does not remember that, so mark values read back as UTC;
        // otherwise JSON has no "Z" and the browser would treat them as local time.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                toDb => toDb.Kind == DateTimeKind.Utc ? toDb : toDb.ToUniversalTime(),
                fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
        {
        }
    }
}
