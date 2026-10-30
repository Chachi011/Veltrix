using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/fairness")]
[AllowAnonymous]
[Produces("application/json")]
public class FairnessController : ControllerBase
{
    private readonly IFairnessService _fairnessService;

    public FairnessController(IFairnessService fairnessService)
    {
        _fairnessService = fairnessService;
    }

    /// <summary>A round's published hash and, once it is over, its seed, crash point and the server's own verification.</summary>
    [HttpGet("rounds/{id:int}")]
    [ProducesResponseType<FairnessRoundDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FairnessRoundDto>> GetRound(int id, CancellationToken ct) =>
        Ok(await _fairnessService.GetRoundAsync(id, ct));

    /// <summary>
    /// Recalculates hash and crash point from a seed. Pass the published hash / crash point to compare.
    /// A logged-in player who verifies a real finished round completes the "Check a round" achievement.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType<VerifyFairnessResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyFairnessResult>> Verify(VerifyFairnessRequest request, CancellationToken ct) =>
        Ok(await _fairnessService.VerifyAsync(request, ct));
}
