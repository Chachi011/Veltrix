using System.Net;
using System.Net.Http.Json;
using Veltrix.Application.DTOs.Assistant;
using Veltrix.Infrastructure.Assistant;
using static Veltrix.Tests.Integration.ApiTestHelpers;

namespace Veltrix.Tests.Integration;

/// <summary>
/// The AI assistant without n8n (the test API has no Assistant:WebhookUrl, like a fresh install): the chat must say
/// "not configured" clearly instead of failing silently.
/// </summary>
[Collection(ApiCollection.Name)]
public class AssistantApiTests
{
    private readonly VeltrixApiFactory _api;

    public AssistantApiTests(VeltrixApiFactory api) => _api = api;

    [Fact]
    public async Task Without_n8n_the_status_says_not_configured_and_a_question_gets_that_message()
    {
        using var client = _api.ClientFor(await _api.RegisterPlayerAsync());

        var status = await (await client.GetAsync("api/assistant/status")).ReadAsync<AssistantStatusDto>(HttpStatusCode.OK);
        Assert.False(status.Configured);

        var response = await client.PostAsJsonAsync("api/assistant/messages", new AskAssistantRequest { Message = "How does Crash work?" }, Json);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadProblemAsync(HttpStatusCode.ServiceUnavailable);
        Assert.Equal(N8nAssistantClient.NotConfiguredMessage, problem.GetProperty("detail").GetString());

        // Nothing is saved for a question the AI did not answer
        var history = await (await client.GetAsync("api/assistant/messages")).ReadAsync<List<AssistantMessageDto>>(HttpStatusCode.OK);
        Assert.Empty(history);
    }

    [Fact]
    public async Task The_status_is_for_players_only_like_the_chat()
    {
        using var admin = _api.ClientFor(await _api.LoginSeededAsync("admin"));
        using var guest = _api.ClientFor(null);

        await (await admin.GetAsync("api/assistant/status")).ShouldBeAsync(HttpStatusCode.Forbidden);
        await (await guest.GetAsync("api/assistant/status")).ShouldBeAsync(HttpStatusCode.Unauthorized);
    }
}
