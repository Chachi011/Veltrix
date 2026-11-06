namespace Veltrix.Domain.Enums;

/// <summary>Who wrote a message in the AI assistant chat.</summary>
public enum AssistantMessageRole
{
    /// <summary>The player's question.</summary>
    User = 1,

    /// <summary>The AI assistant's answer.</summary>
    Assistant = 2
}
