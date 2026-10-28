using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Jobs;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Tests.Services;

/// <summary>Preferences, content pages and the daily-bonus reminder job.</summary>
public class ContentAndPreferencesServiceTests : IDisposable
{
    private readonly ServiceTestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private ProfileService Profiles() => new(
        _ctx.Db,
        _ctx.CurrentUser.Object,
        Mock.Of<IPasswordHasher>(),
        Mock.Of<IAvatarStorage>(),
        _ctx.Notifier.Object,
        _ctx.Time,
        NullLogger<ProfileService>.Instance);

    [Fact]
    public async Task Preferences_StartWithTheDefaults_AndAreSavedPerAccount()
    {
        var player = _ctx.AddUser("settings_fan");
        _ctx.ActAs(player);

        var defaults = await Profiles().GetMyPreferencesAsync();
        Assert.Equal(ThemePreference.System, defaults.Theme);
        Assert.True(defaults.SoundEnabled);
        Assert.True(defaults.AnimationsEnabled);
        Assert.Equal("ka", defaults.Language); // Georgian is the default UI language

        await Profiles().UpdateMyPreferencesAsync(new UpdatePreferencesRequest
        {
            Theme = ThemePreference.Light,
            SoundEnabled = false,
            AnimationsEnabled = false,
            Language = "en"
        });

        // A new request (e.g. another device) sees the saved values
        var saved = await Profiles().GetMyPreferencesAsync();
        Assert.Equal(ThemePreference.Light, saved.Theme);
        Assert.False(saved.SoundEnabled);
        Assert.False(saved.AnimationsEnabled);
        Assert.Equal("en", saved.Language);
        Assert.Equal(ServiceTestContext.Now, saved.UpdatedAt);
    }

    [Fact]
    public async Task ContentPage_IsServedFromTheSeededTable_WithEnglishAsTheFallback()
    {
        // Regression: the fallback once followed the UI default language; when that became Georgian,
        // every page (stored in English only) answered 404
        var pages = new StaticPageService(_ctx.Db);

        var rules = await pages.GetAsync("rules", null);
        var georgian = await pages.GetAsync("no-winning-strategy", "ka"); // not translated yet

        Assert.Equal("Crash game rules", rules.Title);
        Assert.Contains("Cash out", rules.Content);
        Assert.Equal("en", georgian.Language);
        await Assert.ThrowsAsync<NotFoundException>(() => pages.GetAsync("does-not-exist", "en"));
    }

    [Fact]
    public async Task DailyBonusReminder_TellsEachPlayerOncePerBonus_AndSkipsStaffBlockedAndPaused()
    {
        var ready = _ctx.AddUser("ready");
        _ctx.AddUser("staff", UserRole.Operator);
        var blocked = _ctx.AddUser("blocked");
        var paused = _ctx.AddUser("paused");
        blocked.IsBlocked = true;
        paused.Limit!.SelfExcludedUntil = ServiceTestContext.Now.AddDays(1);
        await _ctx.Db.SaveChangesAsync();

        // The job creates its own DI scope per run, like in the real app
        var services = new ServiceCollection()
            .AddScoped(_ => ServiceTestContext.CreateDb(_ctx.DatabaseName))
            .BuildServiceProvider();
        var job = new DailyBonusReminderJob(services.GetRequiredService<IServiceScopeFactory>(), _ctx.Notifier.Object, _ctx.Time,
            NullLogger<DailyBonusReminderJob>.Instance);

        await job.RunOnceAsync(CancellationToken.None);
        await job.RunOnceAsync(CancellationToken.None); // a minute later: nothing new

        _ctx.Notifier.Verify(
            n => n.NotificationAsync(ready.Id, It.Is<NotificationDto>(d => d.Type == NotificationTypes.DailyBonus)),
            Times.Once);
        _ctx.Notifier.VerifyNoOtherCalls(); // not the operator, not the blocked or self-excluded player
    }
}
