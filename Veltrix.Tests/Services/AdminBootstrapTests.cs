using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Veltrix.Application.Validators;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Auth;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Tests.Services;

/// <summary>
/// Production has no test accounts: the first admin comes from the server's .env (Database:AdminEmail / AdminPassword)
/// and is created on the first start only, never weak, never by promoting an existing account.
/// </summary>
public class AdminBootstrapTests : IDisposable
{
    private const string Email = "owner@veltrix.example";
    private const string StrongPassword = "Str0ngAdminPass";

    private readonly ServiceTestContext _ctx = new();
    private readonly BCryptPasswordHasher _hasher = new();

    public void Dispose() => _ctx.Dispose();

    private Task StartAsync(string? email = Email, string? password = StrongPassword, string nickname = "admin") =>
        new DatabaseInitializer(
                _ctx.Db,
                _hasher,
                _ctx.Time,
                Options.Create(new DatabaseOptions { AdminEmail = email, AdminPassword = password, AdminNickname = nickname }),
                new RegisterRequestValidator(_ctx.Time),
                NullLogger<DatabaseInitializer>.Instance)
            .InitializeAsync();

    private async Task<int> AdminCountAsync()
    {
        await using var fresh = _ctx.Fresh();
        return await fresh.Users.CountAsync(u => u.Role == UserRole.Admin);
    }

    [Fact]
    public async Task FirstStart_CreatesExactlyOneAdmin_ThatCanLogIn_AndTheNextStartAddsNothing()
    {
        await StartAsync();
        await StartAsync();

        await using var fresh = _ctx.Fresh();
        var admin = await fresh.Users.Include(u => u.Wallet).SingleAsync(u => u.Role == UserRole.Admin);
        Assert.Equal(Email, admin.Email);
        Assert.Equal("admin", admin.Nickname);
        Assert.True(_hasher.Verify(StrongPassword, admin.PasswordHash));
        Assert.NotEqual(StrongPassword, admin.PasswordHash);
        Assert.Equal(0m, admin.Wallet!.Balance); // staff do not bet: no welcome balance
        Assert.Equal(1, await fresh.Users.CountAsync());
    }

    [Fact]
    public async Task WhenAnAdminAlreadyExists_TheEnvValuesAreIgnored()
    {
        _ctx.AddUser("first_admin", UserRole.Admin);

        await StartAsync(email: "someone.else@veltrix.example", password: "An0therPassword");

        Assert.Equal(1, await AdminCountAsync());
        await using var fresh = _ctx.Fresh();
        Assert.False(await fresh.Users.AnyAsync(u => u.Email == "someone.else@veltrix.example"));
    }

    [Theory]
    [InlineData("short1A")]          // too short
    [InlineData("alllowercase1")]    // no upper case letter
    [InlineData("NoDigitsHere")]     // no digit
    public async Task AWeakPassword_StopsTheStart_AndCreatesNothing(string weak)
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(password: weak));

        Assert.Contains("not valid", error.Message);
        Assert.DoesNotContain(weak, error.Message); // the password itself is never written to the log
        Assert.Equal(0, await AdminCountAsync());
    }

    [Fact]
    public async Task AnInvalidEmailOrNickname_StopsTheStart()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(email: "not-an-email"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(nickname: "no spaces allowed"));
        Assert.Equal(0, await AdminCountAsync());
    }

    [Theory]
    [InlineData(Email, null)]
    [InlineData(null, StrongPassword)]
    public async Task OnlyOneOfEmailAndPassword_StopsTheStart(string? email, string? password)
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(email, password));

        Assert.Contains("must be set together", error.Message);
    }

    [Fact]
    public async Task NotConfigured_CreatesNothing()
    {
        await StartAsync(email: null, password: null);

        Assert.Equal(0, await AdminCountAsync());
    }

    [Fact]
    public async Task AnExistingAccount_IsNeverPromotedSilently()
    {
        var player = _ctx.AddUser("lucky");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync(email: player.Email));

        Assert.Contains("already used", error.Message);
        await using var fresh = _ctx.Fresh();
        Assert.Equal(UserRole.Player, (await fresh.Users.SingleAsync(u => u.Id == player.Id)).Role);
        Assert.Equal(0, await AdminCountAsync());
    }
}
