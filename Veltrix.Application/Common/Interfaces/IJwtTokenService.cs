using Veltrix.Domain.Entities;

namespace Veltrix.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user);

    /// <summary>Cryptographically random opaque string (not a JWT).</summary>
    string GenerateRefreshToken();

    /// <summary>SHA-256 hex hash used to store refresh tokens in the DB.</summary>
    string HashToken(string token);

    TimeSpan RefreshTokenLifetime { get; }
}
