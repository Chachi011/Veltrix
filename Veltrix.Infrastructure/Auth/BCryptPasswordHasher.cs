using Veltrix.Application.Common.Interfaces;

namespace Veltrix.Infrastructure.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    // 2^11 rounds: slow enough to make brute force expensive, fast enough (~100 ms) for login
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
