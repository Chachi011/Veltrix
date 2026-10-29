using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Veltrix.Application.DTOs.Auth;

namespace Veltrix.Tests.Integration;

/// <summary>Small helpers so the tests read like the user's actions: register, log in, call the API.</summary>
internal static class ApiTestHelpers
{
    public const string PlayerPassword = "Player123!";

    /// <summary>Same JSON rules as the API (camelCase, enums as text).</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static int _userCount;

    /// <summary>Registers a brand-new adult player (starting balance from the settings, 1000 VLX by default).</summary>
    public static async Task<AuthResponse> RegisterPlayerAsync(this VeltrixApiFactory api, string? nickname = null)
    {
        nickname ??= $"it{Interlocked.Increment(ref _userCount)}_{Guid.NewGuid().ToString("N")[..6]}";
        using var client = api.CreateApiClient();
        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = $"{nickname}@tests.dev",
            Nickname = nickname,
            Password = PlayerPassword,
            DateOfBirth = new DateOnly(1999, 4, 12),
            IsAdultConfirmed = true
        }, Json);
        return await response.ReadAsync<AuthResponse>(HttpStatusCode.OK);
    }

    public static async Task<AuthResponse> LoginAsync(this VeltrixApiFactory api, string email, string password)
    {
        using var client = api.CreateApiClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Password = password }, Json);
        return await response.ReadAsync<AuthResponse>(HttpStatusCode.OK);
    }

    /// <summary>Logs in one of the seeded development accounts (admin, operator, player1, player2).</summary>
    public static Task<AuthResponse> LoginSeededAsync(this VeltrixApiFactory api, string nickname) => nickname switch
    {
        "admin" => api.LoginAsync("admin@veltrix.dev", "Admin123!"),
        "operator" => api.LoginAsync("operator@veltrix.dev", "Operator123!"),
        _ => api.LoginAsync($"{nickname}@veltrix.dev", PlayerPassword)
    };

    public static HttpClient ClientFor(this VeltrixApiFactory api, AuthResponse? auth) => api.CreateApiClient(auth?.AccessToken);

    /// <summary>Checks the status code (showing the body if it is wrong) and reads the JSON body.</summary>
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response, HttpStatusCode expected)
    {
        await response.ShouldBeAsync(expected);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static async Task ShouldBeAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} returned {(int)response.StatusCode} " +
                        $"instead of {(int)expected}. Body: {body}");
        }
    }

    /// <summary>Reads a ProblemDetails error body (every error of the API has this shape).</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        await response.ShouldBeAsync(expected);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        Assert.True(mediaType == "application/problem+json",
            $"Expected application/problem+json but got {mediaType}. Body: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }
}
