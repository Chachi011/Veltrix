using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

/// <summary>Responsible play tools. Players only, like betting itself.</summary>
[ApiController]
[Route("api/limits")]
[Authorize(Roles = RoleNames.Player)]
[Produces("application/json")]
public class LimitsController : ControllerBase
{
    private readonly ILimitsService _limitsService;

    public LimitsController(ILimitsService limitsService)
    {
        _limitsService = limitsService;
    }

    /// <summary>My daily bet limit (and any waiting increase), what I bet today, session reminder, self-exclusion.</summary>
    [HttpGet]
    [ProducesResponseType<PlayerLimitsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerLimitsDto>> Get(CancellationToken ct) =>
        Ok(await _limitsService.GetMyLimitsAsync(ct));

    /// <summary>
    /// Sets the daily bet limit (null = no limit) and the session reminder (0 = off, 15-600 minutes).
    /// A LOWER limit applies at once; a higher limit or removing it applies only after 24 hours.
    /// </summary>
    [HttpPut]
    [ProducesResponseType<PlayerLimitsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerLimitsDto>> Update(UpdateLimitsRequest request, CancellationToken ct) =>
        Ok(await _limitsService.UpdateMyLimitsAsync(request, ct));

    /// <summary>
    /// Pauses betting for Hours24, Days7 or Days30. It can be extended but never shortened or cancelled.
    /// I can still sign in and see my history.
    /// </summary>
    [HttpPost("self-exclusion")]
    [ProducesResponseType<PlayerLimitsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerLimitsDto>> SelfExclude(SelfExclusionRequest request, CancellationToken ct) =>
        Ok(await _limitsService.StartSelfExclusionAsync(request, ct));
}
