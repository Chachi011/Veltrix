using Veltrix.Application.DTOs.Assistant;

namespace Veltrix.Application.Interfaces;

/// <summary>The AI assistant chat of the logged-in player.</summary>
public interface IAssistantService
{
    /// <summary>My conversation, oldest first (the latest messages only).</summary>
    Task<IReadOnlyList<AssistantMessageDto>> GetMyHistoryAsync(CancellationToken ct = default);

    /// <summary>Asks the AI. The question and the answer are saved together, only when the answer arrived.</summary>
    Task<AssistantReplyDto> AskAsync(AskAssistantRequest request, CancellationToken ct = default);

    /// <summary>Deletes my whole conversation. Returns how many messages were deleted.</summary>
    Task<int> ClearMyHistoryAsync(CancellationToken ct = default);
}
