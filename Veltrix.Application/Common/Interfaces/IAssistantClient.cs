namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// Sends one question to the AI and returns its answer. The real implementation posts to an n8n webhook;
/// tests use a fake. Throws ServiceUnavailableException when the AI cannot answer (down, too slow, not set up).
/// </summary>
public interface IAssistantClient
{
    Task<string> AskAsync(AssistantRequest request, CancellationToken ct = default);
}

/// <summary>An earlier message of the conversation: Role is "user" or "assistant" (the names AI models use).</summary>
public sealed record AssistantTurn(string Role, string Text);

/// <summary>
/// Everything the AI needs for one answer. The service builds it; the client only sends it.
/// SessionId is stable per player, so an n8n memory node could use it, but the history is sent anyway
/// (the database is the source of truth, so the workflow can stay stateless).
/// </summary>
public sealed record AssistantRequest(
    string SessionId,
    string Language,
    string SystemPrompt,
    IReadOnlyList<AssistantTurn> History,
    string Message);
