using System.ComponentModel.DataAnnotations;

namespace Veltrix.Infrastructure.Auth;

/// <summary>Bound from the "Jwt" section of configuration.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signing key. Secret: comes from user-secrets / environment variable, never from Git.
    /// 32+ characters = 256+ bits, the minimum for HS256.
    /// </summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters. Set it with 'dotnet user-secrets set Jwt:Key <value>'.")]
    public string Key { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
