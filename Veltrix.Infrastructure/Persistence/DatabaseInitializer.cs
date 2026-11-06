using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Auth;
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

    /// <summary>Create 15 demo players with 30 days of rounds and bets, once (development/demo only, see DemoDataSeeder).</summary>
    public bool SeedDemoData { get; set; }

    /// <summary>
    /// Production: the email of the ONE admin account created on the first start, when no admin exists yet.
    /// Set together with AdminPassword (from the server's .env file); empty = no admin is created.
    /// </summary>
    public string? AdminEmail { get; set; }

    /// <summary>Password of that admin account. Must pass the same rules as a registration. Never logged.</summary>
    public string? AdminPassword { get; set; }

    public string AdminNickname { get; set; } = "admin";
}

/// <summary>
/// Reference data (the Crash game, system settings) is seeded by the migration via HasData().
/// Test USERS and the production admin are created here at runtime, because their passwords must be hashed with BCrypt.
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
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        TimeProvider time,
        IOptions<DatabaseOptions> options,
        IValidator<RegisterRequest> registerValidator,
        ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _time = time;
        _options = options.Value;
        _registerValidator = registerValidator;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_options.MigrateOnStartup)
        {
            _logger.LogInformation("Applying database migrations...");
            await _db.Database.MigrateAsync(ct);
        }

        await EnsureAdminAsync(ct);

        if (_options.SeedTestUsers)
            await SeedTestUsersAsync(ct);

        if (_options.SeedDemoData)
            await new DemoDataSeeder(_db, _passwordHasher, _time, _logger).SeedAsync(ct);
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

    /// <summary>
    /// Production has no test accounts, so the first admin comes from the server's configuration (.env). Only on the
    /// first start: once any admin exists, nothing happens (the password is then changed in the app, not in .env).
    /// Wrong settings stop the start-up with a clear message instead of creating a weak or half-configured admin.
    /// </summary>
    private async Task EnsureAdminAsync(CancellationToken ct)
    {
        var email = _options.AdminEmail?.Trim() ?? string.Empty;
        var password = _options.AdminPassword ?? string.Empty;
        if (email.Length == 0 && password.Length == 0)
            return;
        if (email.Length == 0 || password.Length == 0)
            throw new InvalidOperationException("Database:AdminEmail and Database:AdminPassword must be set together.");

        if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
        {
            _logger.LogInformation("An admin account exists; Database:AdminEmail is only used on the first start");
            return;
        }

        // The same rules as a registration (email, nickname, strong password), so production never gets a weak admin
        var nickname = _options.AdminNickname.Trim();
        var check = await _registerValidator.ValidateAsync(new RegisterRequest
        {
            Email = email,
            Nickname = nickname,
            Password = password,
            DateOfBirth = new DateOnly(2000, 1, 1),
            IsAdultConfirmed = true
        }, ct);
        if (!check.IsValid)
        {
            var problems = string.Join(" ", check.Errors.Select(e => e.ErrorMessage));
            throw new InvalidOperationException(
                $"The admin account in Database:AdminEmail / AdminPassword / AdminNickname is not valid: {problems}");
        }

        // Never turn an existing account into an admin silently
        if (await _db.Users.AnyAsync(u => u.Email == email || u.Nickname == nickname, ct))
            throw new InvalidOperationException($"Cannot create the admin account: the email or the nickname '{nickname}' is already used.");

        // Staff do not bet, so the admin starts with an empty wallet (no welcome balance)
        _db.Users.Add(User.Create(email, nickname, _passwordHasher.Hash(password), new DateOnly(2000, 1, 1), UserRole.Admin, 0m,
            _time.GetUtcNow().UtcDateTime));
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created the admin account {Email} ({Nickname})", email, nickname);
    }
}
