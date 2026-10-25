using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Auth;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IEmailSender emailSender,
        ICurrentUserService currentUser,
        TimeProvider time,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _emailSender = emailSender;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var nickname = request.Nickname.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("This email is already registered.");

        if (await _db.Users.AnyAsync(u => u.Nickname == nickname, ct))
            throw new ConflictException("This nickname is already taken.");

        var startingBalance = await SettingsReader.GetDecimalAsync(
            _db, SettingKeys.StartingBalance, SettingDefaults.StartingBalance, ct);

        // Players can only register as Player. Roles are changed by an Admin.
        var user = User.Create(
            email,
            nickname,
            _passwordHasher.Hash(request.Password),
            request.DateOfBirth,
            UserRole.Player,
            startingBalance,
            Now);

        user.RegisterSuccessfulLogin(Now);

        // The JWT needs user.Id, which SQL Server only generates on the first save.
        // An explicit transaction keeps "user + wallet + welcome bonus + limits + refresh token" all-or-nothing.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var response = IssueTokens(user);
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        _logger.LogInformation("User {UserId} ({Nickname}) registered with {Balance} VLX", user.Id, user.Nickname, startingBalance);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same message for "no such user" and "wrong password": do not reveal which emails exist
        if (user is null)
        {
            await RecordLoginAttemptAsync(null, email, "UnknownEmail", ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.IsBlocked)
        {
            await RecordLoginAttemptAsync(user.Id, email, "Blocked", ct);
            throw new ForbiddenException("Your account is blocked. Contact support.");
        }

        if (user.IsLockedOut(Now))
        {
            await RecordLoginAttemptAsync(user.Id, email, "LockedOut", ct);
            throw new AccountLockedException(user.LockoutUntil!.Value);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(Now);
            // Saves the failed-login counter and the attempt row together
            await RecordLoginAttemptAsync(user.Id, email, "WrongPassword", ct);

            if (user.IsLockedOut(Now))
            {
                _logger.LogWarning("User {UserId} locked out after {Attempts} failed logins", user.Id, User.MaxFailedLoginAttempts);
                throw new AccountLockedException(user.LockoutUntil!.Value);
            }

            throw new UnauthorizedException("Invalid email or password.");
        }

        user.RegisterSuccessfulLogin(Now);
        var response = IssueTokens(user);
        AddLoginAttempt(user.Id, email, failureReason: null);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = _jwt.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            throw new UnauthorizedException("Invalid refresh token.");

        var user = stored.User!;

        if (stored.RevokedAt is not null)
        {
            // Two tabs refreshing at once, or a page reloaded before it got the new token:
            // the same token arrives again seconds after its rotation. Not an attack, so issue a fresh pair.
            if (stored.IsRecentlyRotated(Now) && !user.IsBlocked)
            {
                _logger.LogInformation("Refresh token of user {UserId} reused within the grace period (concurrent refresh)", user.Id);
                var concurrentResponse = IssueTokens(user);
                await _db.SaveChangesAsync(ct);
                return concurrentResponse;
            }

            // An old ROTATED token is being replayed: it may have been stolen.
            // Safest reaction: log the user out everywhere.
            if (stored.ReplayLooksLikeTheft(Now))
            {
                await RevokeAllTokensAsync(stored.UserId, RefreshToken.ReasonReuseDetected, ct);
                _logger.LogWarning("Reuse of revoked refresh token detected for user {UserId}; all sessions revoked", stored.UserId);
                throw new UnauthorizedException("Refresh token is no longer valid. Please log in again.");
            }

            // Revoked by logout, password change or a block: simply no longer valid (not a theft alarm)
            if (user.IsBlocked)
                throw new ForbiddenException("Your account is blocked. Contact support.");

            throw new UnauthorizedException("Refresh token is no longer valid. Please log in again.");
        }

        if (stored.ExpiresAt <= Now)
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");

        if (user.IsBlocked)
            throw new ForbiddenException("Your account is blocked. Contact support.");

        // Rotation: every refresh token can be used exactly once
        stored.Revoke(Now, RefreshToken.ReasonRotated);
        var response = IssueTokens(user);
        await _db.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = _jwt.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is { RevokedAt: null })
        {
            stored.Revoke(Now, RefreshToken.ReasonLogout);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("User {UserId} logged out", stored.UserId);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Silently do nothing for unknown emails, so attackers cannot discover registered emails
        if (user is null || user.IsBlocked)
            return;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        // Only the newest code is valid: a new request invalidates the older ones
        await _db.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, Now), ct);

        _db.PasswordResetCodes.Add(new PasswordResetCode
        {
            UserId = user.Id,
            CodeHash = _passwordHasher.Hash(code),
            CreatedAt = Now,
            ExpiresAt = Now.Add(ResetCodeLifetime)
        });
        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(
            user.Email,
            "VELTRIX password reset code",
            $"Your password reset code is {code}. It expires in {ResetCodeLifetime.TotalMinutes:0} minutes.",
            ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        const string invalidCodeMessage = "The reset code is invalid or has expired.";

        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new BadRequestException(invalidCodeMessage);

        var resetCode = await _db.PasswordResetCodes
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (resetCode is null || !resetCode.CanBeUsed(Now))
            throw new BadRequestException(invalidCodeMessage);

        if (!_passwordHasher.Verify(request.Code, resetCode.CodeHash))
        {
            // Only 5 guesses per code, so a 6-digit code cannot be brute-forced
            resetCode.Attempts++;
            await _db.SaveChangesAsync(ct);
            throw new BadRequestException(invalidCodeMessage);
        }

        resetCode.UsedAt = Now;
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        await _db.SaveChangesAsync(ct);

        // New password = log out every device that used the old one
        await RevokeAllTokensAsync(user.Id, RefreshToken.ReasonPasswordChanged, ct);

        _logger.LogInformation("User {UserId} reset their password", user.Id);
    }

    /// <summary>
    /// Creates the access token and adds a new refresh token to the DbContext.
    /// The caller saves, so the token is stored in the same transaction as the other changes.
    /// </summary>
    private AuthResponse IssueTokens(User user)
    {
        var accessToken = _jwt.CreateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();
        var refreshExpiresAt = Now.Add(_jwt.RefreshTokenLifetime);

        user.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = _jwt.HashToken(refreshToken),
            CreatedAt = Now,
            ExpiresAt = refreshExpiresAt
        });

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Nickname = user.Nickname,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl
            }
        };
    }

    private void AddLoginAttempt(int? userId, string email, string? failureReason) =>
        _db.LoginAttempts.Add(new LoginAttempt
        {
            UserId = userId,
            Email = email,
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            Succeeded = failureReason is null,
            FailureReason = failureReason,
            CreatedAt = Now
        });

    private Task RecordLoginAttemptAsync(int? userId, string email, string failureReason, CancellationToken ct)
    {
        AddLoginAttempt(userId, email, failureReason);
        return _db.SaveChangesAsync(ct);
    }

    private Task RevokeAllTokensAsync(int userId, string reason, CancellationToken ct) =>
        _db.RefreshTokens.RevokeAllForUserAsync(userId, Now, reason, ct);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
