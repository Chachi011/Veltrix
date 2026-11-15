using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Infrastructure.Assistant;

namespace Veltrix.Tests.Services;

/// <summary>The n8n client (with a fake HTTP handler, no real n8n) and the rules in the AI's instructions.</summary>
public class AssistantClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return await _respond(request, ct);
        }
    }

    private static readonly AssistantRequest Question = new(
        "veltrix-user-7", "en", "RULES", new[] { new AssistantTurn("user", "Hi"), new AssistantTurn("assistant", "Hello!") }, "What is VLX?");

    private static N8nAssistantClient Client(FakeHandler handler, string url = "http://n8n.test/webhook/veltrix-assistant", string? secret = "s3cret", int timeoutSeconds = 30) =>
        new(new HttpClient(handler),
            Options.Create(new AssistantOptions { WebhookUrl = url, Secret = secret, TimeoutSeconds = timeoutSeconds }),
            NullLogger<N8nAssistantClient>.Instance);

    private static FakeHandler Answering(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }));

    [Fact]
    public async Task Posts_the_question_with_the_secret_and_reads_the_reply()
    {
        var handler = Answering(HttpStatusCode.OK, """{ "reply": "  VLX are virtual points.  " }""");

        var reply = await Client(handler).AskAsync(Question);

        Assert.Equal("VLX are virtual points.", reply);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("s3cret", handler.Request.Headers.GetValues(N8nAssistantClient.SecretHeader).Single());

        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("veltrix-user-7", body.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("en", body.RootElement.GetProperty("language").GetString());
        Assert.Equal("RULES", body.RootElement.GetProperty("systemPrompt").GetString());
        Assert.Equal("What is VLX?", body.RootElement.GetProperty("message").GetString());
        Assert.Equal("assistant", body.RootElement.GetProperty("history")[1].GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("""{ "reply": "A" }""", "A")]
    [InlineData("""{ "output": "A" }""", "A")]          // the AI Agent node's own field
    [InlineData("""[ { "output": "A" } ]""", "A")]      // Respond to Webhook: all incoming items
    [InlineData("A plain text answer", "A plain text answer")]
    [InlineData("""{ "something": "else" }""", null)]
    [InlineData("", null)]
    public void Reads_the_reply_from_the_usual_n8n_shapes(string body, string? expected)
    {
        Assert.Equal(expected, N8nAssistantClient.ReadReply(body));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, """{ "reply": "x" }""")]
    [InlineData(HttpStatusCode.Unauthorized, "")]          // wrong secret
    [InlineData(HttpStatusCode.NotFound, "")]              // workflow not active
    [InlineData(HttpStatusCode.OK, """{ "reply": "" }""")] // empty answer
    public async Task A_failed_call_becomes_one_friendly_503(HttpStatusCode status, string body)
    {
        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Client(Answering(status, body)).AskAsync(Question));

        Assert.Equal(503, error.StatusCode);
        Assert.Equal(N8nAssistantClient.UnavailableMessage, error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task No_webhook_url_means_the_assistant_is_off_and_says_it_is_not_configured(string url)
    {
        var handler = Answering(HttpStatusCode.OK, """{ "reply": "x" }""");
        var client = Client(handler, url: url);

        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => client.AskAsync(Question));

        Assert.False(client.IsConfigured);
        Assert.Equal(503, error.StatusCode);
        // Its own message: "try again in a moment" would be wrong, waiting does not help
        Assert.Equal(N8nAssistantClient.NotConfiguredMessage, error.Message);
        Assert.Null(handler.Request); // nothing was sent
    }

    [Fact]
    public void A_webhook_url_means_the_assistant_is_configured()
    {
        Assert.True(Client(Answering(HttpStatusCode.OK, "")).IsConfigured);
    }

    [Fact]
    public async Task Unreachable_n8n_becomes_a_503()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(() => Client(handler).AskAsync(Question));
    }

    [Fact]
    public async Task A_slow_workflow_times_out_as_a_503()
    {
        var handler = new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // 5 seconds is the shortest timeout the options allow
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => Client(handler, timeoutSeconds: 1).AskAsync(Question));
    }

    [Fact]
    public void The_instructions_always_forbid_strategies_and_invitations_to_play()
    {
        var prompt = AssistantPrompts.Build("en", "nova", canPlayNow: true, isSelfExcluded: false);

        Assert.Contains(AssistantPrompts.NoStrategyRule, prompt);
        Assert.Contains(AssistantPrompts.NoInvitationRule, prompt);
        Assert.Contains(AssistantPrompts.ProblemPlayRule, prompt);
        Assert.Contains("VIRTUAL points with no money value", prompt);
        Assert.DoesNotContain(AssistantPrompts.BettingPausedRule, prompt);
    }

    [Fact]
    public void The_instructions_ask_for_Georgian_for_a_Georgian_player()
    {
        var prompt = AssistantPrompts.Build("ka", "nova", canPlayNow: false, isSelfExcluded: false);

        Assert.Contains("natural Georgian", prompt);
        Assert.Contains(AssistantPrompts.BettingPausedRule, prompt);
        Assert.DoesNotContain(AssistantPrompts.SelfExcludedNote, prompt);
    }
}
