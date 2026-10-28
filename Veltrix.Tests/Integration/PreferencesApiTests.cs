using System.Net;
using System.Net.Http.Json;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Domain.Enums;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>Settings saved on the account (spec 4.7), including the UI language: Georgian by default.</summary>
[Collection(ApiCollection.Name)]
public class PreferencesApiTests
{
    private readonly VeltrixApiFactory _api;

    public PreferencesApiTests(VeltrixApiFactory api) => _api = api;

    [Fact]
    public async Task A_new_account_starts_in_Georgian_and_can_switch_to_English()
    {
        using var client = _api.ClientFor(await _api.RegisterPlayerAsync());

        var initial = await (await client.GetAsync("api/profile/preferences")).ReadAsync<UserPreferencesDto>(HttpStatusCode.OK);
        Assert.Equal("ka", initial.Language);

        var request = new UpdatePreferencesRequest { Theme = ThemePreference.Dark, SoundEnabled = false, AnimationsEnabled = true, Language = "en" };
        await (await client.PutAsJsonAsync("api/profile/preferences", request, Json)).ShouldBeAsync(HttpStatusCode.OK);

        var saved = await (await client.GetAsync("api/profile/preferences")).ReadAsync<UserPreferencesDto>(HttpStatusCode.OK);
        Assert.Equal("en", saved.Language);
        Assert.Equal(ThemePreference.Dark, saved.Theme);
        Assert.False(saved.SoundEnabled);
    }

    [Fact]
    public async Task Only_Georgian_and_English_are_accepted()
    {
        using var client = _api.ClientFor(await _api.RegisterPlayerAsync());

        var request = new UpdatePreferencesRequest { Theme = ThemePreference.System, Language = "fr" };
        var problem = await (await client.PutAsJsonAsync("api/profile/preferences", request, Json)).ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("language", out _));
    }
}
