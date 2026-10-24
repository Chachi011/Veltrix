using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class User
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;
    public string? AvatarUrl { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public bool IsBlocked { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet? Wallet { get; set; }
    public PlayerLimit? Limit { get; set; }
    public UserPreference? Preference { get; set; }
    public PlayerStats? Stats { get; set; }
    public UserTwoFactor? TwoFactor { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    /// <summary>
    /// Creates a user together with everything every user must have:
    /// a wallet with the starting balance (recorded as a Bonus transaction), default limits,
    /// default UI preferences and an empty stats row.
    /// </summary>
    public static User Create(
        string email,
        string nickname,
        string passwordHash,
        DateOnly dateOfBirth,
        UserRole role,
        decimal startingBalance,
        DateTime now)
    {
        var user = new User
        {
            Email = email,
            Nickname = nickname,
            PasswordHash = passwordHash,
            DateOfBirth = dateOfBirth,
            Role = role,
            CreatedAt = now,
            Wallet = new Wallet { Balance = startingBalance },
            Limit = new PlayerLimit { SessionReminderMin = PlayerLimit.DefaultSessionReminderMin },
            Preference = new UserPreference { UpdatedAt = now },
            Stats = new PlayerStats { LevelNumber = 1, UpdatedAt = now }
        };

        if (startingBalance > 0)
        {
            user.Wallet.Transactions.Add(new WalletTransaction
            {
                Type = TransactionType.Bonus,
                Amount = startingBalance,
                BalanceBefore = 0,
                BalanceAfter = startingBalance,
                IdempotencyKey = $"welcome:{email}",
                Reason = "Welcome balance",
                CreatedAt = now
            });
        }

        return user;
    }

    public bool IsLockedOut(DateTime now) => LockoutUntil.HasValue && LockoutUntil.Value > now;

    public void RegisterFailedLogin(DateTime now)
    {
        FailedLoginCount++;

        if (FailedLoginCount >= MaxFailedLoginAttempts)
        {
            LockoutUntil = now.Add(LockoutDuration);
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        FailedLoginCount = 0;
        LockoutUntil = null;
        LastLoginAt = now;
    }
}
