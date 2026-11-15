using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;

namespace Veltrix.Infrastructure.Assistant;

/// <summary>
/// Posts a question to the n8n workflow and reads the answer.
///
/// Request (JSON): { sessionId, language, systemPrompt, history: [{ role, text }], message }
/// Answer: { "reply": "..." } from a Respond to Webhook node. "output" (the AI Agent node's own field), an array of
/// such objects, or plain text are accepted too, so a slightly different workflow still works.
///
/// Every failure becomes a friendly 503 message for the player; the details go to the log only. "Not set up" has
/// its own message, because trying again later does not help then. The question text is never logged
/// (it is the player's private message).
/// </summary>
public sealed class N8nAssistantClient : IAssistantClient
{
    public const string UnavailableMessage = "The assistant is not available right now. Please try again in a moment.";
    public const string NotConfiguredMessage = "The assistant is not configured.";
    public const string SecretHeader = "X-Veltrix-Secret";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] ReplyFields = { "reply", "output", "text", "answer" };

    private readonly HttpClient _http;
    private readonly IOptions<AssistantOptions> _options;
    private readonly ILogger<N8nAssistantClient> _logger;

    public N8nAssistantClient(HttpClient http, IOptions<AssistantOptions> options, ILogger<N8nAssistantClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Value.WebhookUrl);

    public async Task<string> AskAsync(AssistantRequest request, CancellationToken ct = default)
    {
        var options = _options.Value;
        if (!IsConfigured)
        {
            _logger.LogWarning("Assistant:WebhookUrl is not set, so the AI assistant cannot answer");
            throw new ServiceUnavailableException(NotConfiguredMessage);
        }

        // Our own timeout, linked to the request's token: a closed browser tab still cancels the call
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120)));

        using var message = new HttpRequestMessage(HttpMethod.Post, options.WebhookUrl)
        {
            Content = JsonContent.Create(new
            {
                sessionId = request.SessionId,
                language = request.Language,
                systemPrompt = request.SystemPrompt,
                history = request.History.Select(t => new { role = t.Role, text = t.Text }),
                message = request.Message
            }, options: Json)
        };
        if (!string.IsNullOrEmpty(options.Secret))
            message.Headers.Add(SecretHeader, options.Secret);

        try
        {
            using var response = await _http.SendAsync(message, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("The assistant workflow answered {Status}", (int)response.StatusCode);
                throw new ServiceUnavailableException(UnavailableMessage);
            }

            var reply = ReadReply(await response.Content.ReadAsStringAsync(timeout.Token));
            if (string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogWarning("The assistant workflow answered without a reply text");
                throw new ServiceUnavailableException(UnavailableMessage);
            }

            return reply.Trim();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("The assistant workflow did not answer within {Seconds} s", options.TimeoutSeconds);
            throw new ServiceUnavailableException(UnavailableMessage);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning("Cannot reach the assistant workflow: {Error}", exception.Message);
            throw new ServiceUnavailableException(UnavailableMessage);
        }
    }

    /// <summary>The answer text from the workflow's response body, or null if there is none.</summary>
    public static string? ReadReply(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            return FromElement(document.RootElement);
        }
        catch (JsonException)
        {
            // Not JSON: a Respond to Webhook node set to "Text"
            return body;
        }
    }

    private static string? FromElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        // "All Incoming Items" sends an array: the first item is the answer
        JsonValueKind.Array => element.EnumerateArray().Select(FromElement).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)),
        JsonValueKind.Object => ReplyFields
            .Select(name => element.TryGetProperty(name, out var value) ? FromElement(value) : null)
            .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)),
        _ => null
    };
}
