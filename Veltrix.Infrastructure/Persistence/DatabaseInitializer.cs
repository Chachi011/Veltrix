using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF migrations when the API starts.</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Create the test accounts for every role (development/demo only).</summary>
    public bool SeedTestUsers { get; set; }
}

/// <summary>
/// Reference data (the Crash game, system settings) is seeded by the migration via HasData().
/// Test USERS are seeded here at runtime, because their passwords must be hashed with BCrypt.
/// </summary>
public sealed class DatabaseInitializer
{
    public static readonly (string Email, string Nickname, string Password, UserRole Role)[] TestUsers =
    {
        ("admin@veltrix.dev", "admin", "Admin123!", UserRole.Admin),
        ("operator@veltrix.dev", "operator", "Operator123!", UserRole.Operator),
        ("player1@veltrix.dev", "player1", "Player123!", UserRole.Player),
        ("player2@veltrix.dev", "player2", "Player123!", UserRole.Player)
    };

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _time;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        TimeProvider time,
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_options.MigrateOnStartup)
        {
            _logger.LogInformation("Applying database migrations...");
            await _db.Database.MigrateAsync(ct);
        }

        if (_options.SeedTestUsers)
            await SeedTestUsersAsync(ct);
    }

    private async Task SeedTestUsersAsync(CancellationToken ct)
    {
        var startingBalance = await SettingsReader.GetDecimalAsync(_db, SettingKeys.StartingBalance, 1000m, ct);
        var now = _time.GetUtcNow().UtcDateTime;
        var created = 0;

        foreach (var (email, nickname, password, role) in TestUsers)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email, ct))
                continue;

            _db.Users.Add(User.Create(
                email,
                nickname,
                _passwordHasher.Hash(password),
                new DateOnly(2000, 1, 1),
                role,
                startingBalance,
                now));
            created++;
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} test users", created);
        }
    }
}
