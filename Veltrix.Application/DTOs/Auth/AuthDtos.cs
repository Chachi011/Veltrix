using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }

    /// <summary>The "I am 18+" checkbox.</summary>
    public bool IsAdultConfirmed { get; set; }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public sealed record MessageResponse(string Message);
