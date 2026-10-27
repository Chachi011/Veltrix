using Veltrix.Domain.Entities;

namespace Veltrix.Tests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTime Now = TestData.Now;

    private static RefreshToken Token() => new() { CreatedAt = Now.AddMinutes(-5), ExpiresAt = Now.AddDays(7) };

    [Fact]
    public void JustRotatedToken_IsAcceptedAgain_WithinTheGracePeriod()
    {
        var token = Token();
        token.Revoke(Now, RefreshToken.ReasonRotated);

        // e.g. a second tab refreshed with the same token 2 seconds later
        Assert.True(token.IsRecentlyRotated(Now.AddSeconds(2)));
    }

    [Fact]
    public void RotatedToken_ReplayedAfterTheGracePeriod_IsTreatedAsTheft()
    {
        var token = Token();
        token.Revoke(Now, RefreshToken.ReasonRotated);

        Assert.False(token.IsRecentlyRotated(Now + RefreshToken.ReuseGracePeriod + TimeSpan.FromSeconds(1)));
    }

    [Theory]
    [InlineData(RefreshToken.ReasonLogout)]
    [InlineData(RefreshToken.ReasonPasswordChanged)]
    [InlineData(RefreshToken.ReasonReuseDetected)]
    public void TokensRevokedForOtherReasons_NeverGetAGracePeriod(string reason)
    {
        var token = Token();
        token.Revoke(Now, reason);

        Assert.False(token.IsRecentlyRotated(Now.AddSeconds(1)));
    }

    [Fact]
    public void ExpiredToken_NeverGetsAGracePeriod()
    {
        var token = new RefreshToken { CreatedAt = Now.AddDays(-7), ExpiresAt = Now };
        token.Revoke(Now, RefreshToken.ReasonRotated);

        Assert.False(token.IsRecentlyRotated(Now.AddSeconds(1)));
    }

    [Fact]
    public void ActiveToken_IsNotRecentlyRotated()
    {
        Assert.False(Token().IsRecentlyRotated(Now));
    }

    [Fact]
    public void RotatedToken_ReplayedAfterTheGracePeriod_LooksLikeTheft()
    {
        var token = Token();
        token.Revoke(Now, RefreshToken.ReasonRotated);

        Assert.False(token.ReplayLooksLikeTheft(Now.AddSeconds(2)));
        Assert.True(token.ReplayLooksLikeTheft(Now + RefreshToken.ReuseGracePeriod + TimeSpan.FromSeconds(1)));
    }

    // Bug found in Phase 4: an open tab of a just-blocked player refreshed its token, which was reported
    // as "token theft" (warning + revoke everything) instead of "your account is blocked".
    [Theory]
    [InlineData(RefreshToken.ReasonBlocked)]
    [InlineData(RefreshToken.ReasonLogout)]
    [InlineData(RefreshToken.ReasonPasswordChanged)]
    [InlineData(RefreshToken.ReasonReuseDetected)]
    public void TokensRevokedForOtherReasons_AreJustDead_NotATheftAlarm(string reason)
    {
        var token = Token();
        token.Revoke(Now, reason);

        Assert.False(token.ReplayLooksLikeTheft(Now.AddMinutes(5)));
    }
}
