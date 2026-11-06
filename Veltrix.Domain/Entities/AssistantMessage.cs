using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>
/// One message of a player's conversation with the AI assistant: a question or an answer.
/// Each player has one conversation (all their messages, oldest first); they can clear it.
/// </summary>
public class AssistantMessage
{
    /// <summary>A question is short on purpose: the assistant answers questions, it does not read essays.</summary>
    public const int MaxQuestionLength = 500;

    /// <summary>An answer longer than this is cut (the column size); the prompt asks for short answers anyway.</summary>
    public const int MaxTextLength = 4000;

    public int Id { get; set; }
    public int UserId { get; set; }
    public AssistantMessageRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public static AssistantMessage Question(int userId, string text, DateTime now) =>
        Create(userId, AssistantMessageRole.User, text, now);

    public static AssistantMessage Answer(int userId, string text, DateTime now) =>
        Create(userId, AssistantMessageRole.Assistant, text, now);

    private static AssistantMessage Create(int userId, AssistantMessageRole role, string text, DateTime now)
    {
        var trimmed = text.Trim();
        return new AssistantMessage
        {
            UserId = userId,
            Role = role,
            Text = trimmed.Length <= MaxTextLength ? trimmed : trimmed[..MaxTextLength],
            CreatedAt = now
        };
    }
}
