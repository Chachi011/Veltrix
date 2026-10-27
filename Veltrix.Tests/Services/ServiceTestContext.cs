using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Tests.Services;

/// <summary>A clock the test controls.</summary>
internal sealed class FixedTime : TimeProvider
{
    public FixedTime(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}

/// <summary>
/// Everything a service test needs: a fresh in-memory database (with the seeded game, settings and pages),
/// a fixed clock, and Moq fakes for "who is calling" and the SignalR notifier, so a test can check
/// exactly which live events a service sends.
/// </summary>
internal sealed class ServiceTestContext : IDisposable
{
    public static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    public ServiceTestContext()
    {
        DatabaseName = Guid.NewGuid().ToString();
        Db = CreateDb(DatabaseName);
        Db.Database.EnsureCreated(); // applies HasData: the crash game, system settings, levels, content pages
    }

    public string DatabaseName { get; }
    public ApplicationDbContext Db { get; }
    public FixedTime Time { get; } = new(Now);
    public Mock<ICurrentUserService> CurrentUser { get; } = new();
    public Mock<IGameNotifier> Notifier { get; } = new();

    public static ApplicationDbContext CreateDb(string databaseName) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            // The in-memory provider has no real transactions; the services' explicit ones become no-ops
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    /// <summary>A user with everything registration creates (wallet with the starting balance, limits, preferences, stats).</summary>
    public User AddUser(string nickname, UserRole role = UserRole.Player, decimal balance = 1000m)
    {
        var user = User.Create($"{nickname}@test.dev", nickname, "hash", new DateOnly(1995, 5, 5), role, balance, Now.AddDays(-10));
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    /// <summary>The service under test now acts as this user.</summary>
    public void ActAs(User user)
    {
        CurrentUser.Setup(c => c.UserId).Returns(user.Id);
        CurrentUser.Setup(c => c.IsAuthenticated).Returns(true);
    }

    /// <summary>Reads the database again, as the next request would (not the entities the service changed).</summary>
    public ApplicationDbContext Fresh() => CreateDb(DatabaseName);

    public void Dispose() => Db.Dispose();
}
