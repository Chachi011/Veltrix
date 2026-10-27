using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Files;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class ProfileService : IProfileService
{
    private const int BestCashOutsCount = 5;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAvatarStorage _avatarStorage;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        IAvatarStorage avatarStorage,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<ProfileService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _avatarStorage = avatarStorage;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                Nickname = u.Nickname,
                Role = u.Role,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth,
                CreatedAt = u.CreatedAt,
                Balance = u.Wallet != null ? u.Wallet.Balance : 0
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync(ct);
        var nickname = request.Nickname.Trim();

        if (nickname != user.Nickname)
        {
            // The DB collation is case-insensitive, so "Player1" and "player1" count as the same nickname
            var taken = await _db.Users.AnyAsync(u => u.Nickname == nickname && u.Id != user.Id, ct);
            if (taken)
                throw new ConflictException("This nickname is already taken.");

            var oldNickname = user.Nickname;
            user.Nickname = nickname;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} changed nickname {Old} -> {New}", user.Id, oldNickname, nickname);
        }

        return await GetMyProfileAsync(ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await LoadCurrentUserAsync(ct);

        // Asking for the current password stops someone who found an unlocked, logged-in computer
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        // A security notice: if someone else changed the password, the owner sees it on the bell
        var notification = UserNotifications.Add(_db, user.Id, NotificationTypes.PasswordChanged, "Password changed",
            "Your password was changed and all devices were signed out. If this was not you, reset your password now.", Now);
        await _db.SaveChangesAsync(ct);

        // Every device must log in again with the new password
        await _db.RefreshTokens.RevokeAllForUserAsync(user.Id, Now, RefreshToken.ReasonPasswordChanged, ct);
        await _notifier.PushAsync(notification);

        _logger.LogInformation("User {UserId} changed their password; all refresh tokens revoked", user.Id);
    }

    public async Task<ProfileDto> UploadAvatarAsync(byte[] content, CancellationToken ct = default)
    {
        if (content.Length == 0 || content.Length > AvatarRules.MaxBytes)
            throw new BadRequestException("Avatar must be between 1 byte and 2 MB.");

        var extension = ImageFormatDetector.DetectExtension(content)
            ?? throw new BadRequestException("The file is not a valid JPG, PNG or WEBP image.");

        var user = await LoadCurrentUserAsync(ct);
        var oldAvatarUrl = user.AvatarUrl;

        user.AvatarUrl = await _avatarStorage.SaveAsync(content, extension, ct);
        await _db.SaveChangesAsync(ct);

        // Delete the old file only AFTER the DB points to the new one
        if (oldAvatarUrl is not null)
            await _avatarStorage.DeleteAsync(oldAvatarUrl, ct);

        _logger.LogInformation("User {UserId} uploaded a new avatar {AvatarUrl}", user.Id, user.AvatarUrl);
        return await GetMyProfileAsync(ct);
    }

    public async Task<PublicProfileDto> GetPublicProfileAsync(string nickname, CancellationToken ct = default)
    {
        // Blocked accounts are hidden from the public
        var profile = await _db.Users
            .AsNoTracking()
            .Where(u => u.Nickname == nickname && !u.IsBlocked)
            .Select(u => new PublicProfileDto
            {
                Nickname = u.Nickname,
                AvatarUrl = u.AvatarUrl,
                MemberSince = u.CreatedAt,
                Level = u.Stats != null ? u.Stats.LevelNumber : 1,
                LevelTitle = u.Stats != null && u.Stats.Level != null ? u.Stats.Level.Title : string.Empty,
                TotalBets = u.Stats != null ? u.Stats.TotalBets : 0,
                CashOutCount = u.Stats != null ? u.Stats.CashOutCount : 0,
                BiggestMultiplier = u.Stats != null ? u.Stats.BiggestMultiplier : 0,
                BiggestWin = u.Stats != null ? u.Stats.BiggestWin : 0,
                BestCashOuts = u.Bets
                    .Where(b => b.Status == BetStatus.CashedOut)
                    .OrderByDescending(b => b.CashOutMultiplier)
                    .ThenByDescending(b => b.Payout)
                    .Take(BestCashOutsCount)
                    .Select(b => new BestCashOutDto
                    {
                        Multiplier = b.CashOutMultiplier ?? 0,
                        Payout = b.Payout ?? 0,
                        CashedOutAt = b.CashedOutAt ?? b.PlacedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return profile ?? throw new NotFoundException($"Player '{nickname}' was not found.");
    }

    public async Task<UserPreferencesDto> GetMyPreferencesAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var preference = await _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? new UserPreference { UserId = userId, UpdatedAt = Now }; // every account gets a row at registration

        return ToDto(preference);
    }

    public async Task<UserPreferencesDto> UpdateMyPreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var preference = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (preference is null)
        {
            preference = new UserPreference { UserId = userId };
            _db.UserPreferences.Add(preference);
        }

        preference.Theme = request.Theme;
        preference.SoundEnabled = request.SoundEnabled;
        preference.AnimationsEnabled = request.AnimationsEnabled;
        preference.UpdatedAt = Now;
        await _db.SaveChangesAsync(ct);

        return ToDto(preference);
    }

    private static UserPreferencesDto ToDto(UserPreference preference) => new()
    {
        Theme = preference.Theme,
        SoundEnabled = preference.SoundEnabled,
        AnimationsEnabled = preference.AnimationsEnabled,
        UpdatedAt = preference.UpdatedAt
    };

    private async Task<User> LoadCurrentUserAsync(CancellationToken ct)
    {
        var userId = CurrentUserId;
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");
    }
}
