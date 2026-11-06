using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Assistant;

/// <summary>One message of the AI assistant chat (the player's question or the assistant's answer).</summary>
public sealed class AssistantMessageDto
{
    public int Id { get; set; }
    public AssistantMessageRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>POST /api/assistant/messages</summary>
public sealed class AskAssistantRequest
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>The saved question and the assistant's answer to it, so the client can show both with their real ids.</summary>
public sealed class AssistantReplyDto
{
    public AssistantMessageDto Question { get; set; } = new();
    public AssistantMessageDto Answer { get; set; } = new();
}
