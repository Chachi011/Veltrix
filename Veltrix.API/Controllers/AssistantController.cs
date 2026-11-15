using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Veltrix.API.Extensions;
using Veltrix.Application.DTOs.Assistant;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

/// <summary>
/// The AI assistant chat. Players only: it explains the game and the responsible play tools, which only players use.
/// The answers come from an n8n workflow (see docs/n8n/README.md).
/// </summary>
[ApiController]
[Route("api/assistant")]
[Authorize(Roles = RoleNames.Player)]
[Produces("application/json")]
public class AssistantController : ControllerBase
{
    private readonly IAssistantService _assistantService;

    public AssistantController(IAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    /// <summary>Whether the assistant is set up. The chat shows "The assistant is not configured" instead of a dead input box.</summary>
    [HttpGet("status")]
    [ProducesResponseType<AssistantStatusDto>(StatusCodes.Status200OK)]
    public ActionResult<AssistantStatusDto> GetStatus() => Ok(_assistantService.GetStatus());

    /// <summary>My conversation with the assistant, oldest first (the latest 50 messages).</summary>
    [HttpGet("messages")]
    [ProducesResponseType<IReadOnlyList<AssistantMessageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssistantMessageDto>>> GetHistory(CancellationToken ct) =>
        Ok(await _assistantService.GetMyHistoryAsync(ct));

    /// <summary>
    /// Asks the assistant. Returns the saved question and the answer. 503 when the AI cannot answer right now
    /// (nothing is saved then); 429 after 10 questions in a minute.
    /// </summary>
    [HttpPost("messages")]
    [EnableRateLimiting(ApiServiceExtensions.AssistantRateLimitPolicy)]
    [ProducesResponseType<AssistantReplyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AssistantReplyDto>> Ask(AskAssistantRequest request, CancellationToken ct) =>
        Ok(await _assistantService.AskAsync(request, ct));

    /// <summary>Deletes my whole conversation ("New chat").</summary>
    [HttpDelete("messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _assistantService.ClearMyHistoryAsync(ct);
        return NoContent();
    }
}
