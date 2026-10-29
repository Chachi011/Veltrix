using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Veltrix.Application.DTOs.Auth;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>Registration, login, JWT protection, refresh-token rotation and the auth rate limit, over real HTTP.</summary>
[Collection(ApiCollection.Name)]
public class AuthApiTests
{
    private readonly VeltrixApiFactory _api;

    public AuthApiTests(VeltrixApiFactory api) => _api = api;

    [Fact]
    public async Task Register_creates_a_player_with_the_starting_balance_and_working_tokens()
    {
        var auth = await _api.RegisterPlayerAsync("it_newcomer");

        Assert.Equal(UserRole.Player, auth.User.Role);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));

        using var client = _api.ClientFor(auth);
        var profile = await (await client.GetAsync("api/profile")).ReadAsync<ProfileDto>(HttpStatusCode.OK);
        Assert.Equal("it_newcomer", profile.Nickname);

        var wallet = await (await client.GetAsync("api/wallet")).ReadAsync<WalletDto>(HttpStatusCode.OK);
        Assert.Equal(1000m, wallet.Balance);
    }

    [Fact]
    public async Task Register_rejects_invalid_input_with_400_and_an_error_per_field()
    {
        using var client = _api.CreateApiClient();
        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = "not-an-email",
            Nickname = "x",
            Password = "short",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            IsAdultConfirmed = false
        }, Json);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        var fields = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(fields, f => f.Equals("Email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fields, f => f.Equals("Nickname", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fields, f => f.Equals("Password", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fields, f => f.Equals("DateOfBirth", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fields, f => f.Equals("IsAdultConfirmed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_with_an_email_that_is_taken_returns_409()
    {
        await _api.RegisterPlayerAsync("it_original");

        using var client = _api.CreateApiClient();
        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = "it_original@tests.dev",
            Nickname = "it_copycat",
            Password = PlayerPassword,
            DateOfBirth = new DateOnly(1990, 1, 1),
            IsAdultConfirmed = true
        }, Json);

        await response.ReadProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401_and_with_the_right_one_returns_tokens()
    {
        using var client = _api.CreateApiClient();

        var wrong = await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = "player1@veltrix.dev", Password = "Wrong123!" }, Json);
        await wrong.ReadProblemAsync(HttpStatusCode.Unauthorized);

        var right = await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = "player1@veltrix.dev", Password = PlayerPassword }, Json);
        var auth = await right.ReadAsync<AuthResponse>(HttpStatusCode.OK);
        Assert.Equal("player1", auth.User.Nickname);
    }

    [Fact]
    public async Task Protected_endpoints_need_a_valid_access_token()
    {
        using var guest = _api.CreateApiClient();
        await (await guest.GetAsync("api/profile")).ReadProblemAsync(HttpStatusCode.Unauthorized);

        using var forged = _api.CreateApiClient("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwicm9sZSI6IkFkbWluIn0.bm90LXNpZ25lZA");
        await (await forged.GetAsync("api/profile")).ReadProblemAsync(HttpStatusCode.Unauthorized);

        // Public pages stay public
        await (await guest.GetAsync("api/games")).ShouldBeAsync(HttpStatusCode.OK);
        await (await guest.GetAsync("api/leaderboard")).ShouldBeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_logout_kills_it()
    {
        var auth = await _api.RegisterPlayerAsync();
        using var client = _api.CreateApiClient();

        var refreshed = await (await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, Json))
            .ReadAsync<AuthResponse>(HttpStatusCode.OK);
        Assert.NotEqual(auth.RefreshToken, refreshed.RefreshToken);

        using (var withNewToken = _api.ClientFor(refreshed))
            await (await withNewToken.GetAsync("api/profile")).ShouldBeAsync(HttpStatusCode.OK);

        await (await client.PostAsJsonAsync("api/auth/logout", new RefreshTokenRequest { RefreshToken = refreshed.RefreshToken }, Json))
            .ShouldBeAsync(HttpStatusCode.NoContent);

        var afterLogout = await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshed.RefreshToken }, Json);
        await afterLogout.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_rotated_token_may_be_reused_within_30_seconds_but_a_later_replay_logs_out_every_session()
    {
        var auth = await _api.RegisterPlayerAsync();
        using var client = _api.CreateApiClient();
        var refresh = (string token) => client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = token }, Json);

        var newer = await (await refresh(auth.RefreshToken)).ReadAsync<AuthResponse>(HttpStatusCode.OK);

        // Second tab refreshing with the same token a moment later: allowed (grace period)
        await (await refresh(auth.RefreshToken)).ShouldBeAsync(HttpStatusCode.OK);

        // 31 seconds later the old token comes back: someone else has a copy of it.
        // (We move the rotation time back instead of waiting 31 seconds.)
        await _api.WithDbAsync(db => db.RefreshTokens
            .Where(t => t.UserId == auth.User.Id && t.RevokedReason == RefreshToken.ReasonRotated)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, t => t.RevokedAt!.Value.AddSeconds(-31))));

        await (await refresh(auth.RefreshToken)).ReadProblemAsync(HttpStatusCode.Unauthorized);

        // Theft alarm: even the newest token no longer works, everyone must log in again
        await (await refresh(newer.RefreshToken)).ReadProblemAsync(HttpStatusCode.Unauthorized);
        var active = await _api.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == auth.User.Id && t.RevokedAt == null));
        Assert.Equal(0, active);
    }

    [Fact]
    public async Task Auth_rate_limit_allows_10_login_attempts_per_minute_per_IP_but_does_not_limit_refresh()
    {
        var auth = await _api.RegisterPlayerAsync();
        using var client = _api.CreateApiClient();
        var attempt = () => client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = "nobody@tests.dev", Password = "Guess123!" }, Json);

        for (var i = 0; i < 10; i++)
            await (await attempt()).ShouldBeAsync(HttpStatusCode.Unauthorized);

        await (await attempt()).ReadProblemAsync(HttpStatusCode.TooManyRequests);

        // Same IP, but refreshing is not password guessing: a busy tab must never be locked out
        await (await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = auth.RefreshToken }, Json))
            .ShouldBeAsync(HttpStatusCode.OK);
    }
}
