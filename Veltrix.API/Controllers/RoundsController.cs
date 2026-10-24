using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

/// <summary>Read endpoints are public (guests can watch); cancelling needs Operator/Admin.</summary>
[ApiController]
[Produces("application/json")]
public class RoundsController : ControllerBase
{
    private readonly IRoundService _roundService;

    public RoundsController(IRoundService roundService)
    {
        _roundService = roundService;
    }

    /// <summary>The round being played right now (seed and crash point hidden until it is over).</summary>
    [AllowAnonymous]
    [HttpGet("api/games/{slug}/rounds/current")]
    [ProducesResponseType<RoundDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoundDto>> GetCurrent(string slug, CancellationToken ct) =>
        Ok(await _roundService.GetCurrentAsync(slug, ct));

    /// <summary>Finished and cancelled rounds, newest first (use pageSize=20 for the "last 20 crashes" strip).</summary>
    [AllowAnonymous]
    [HttpGet("api/games/{slug}/rounds")]
    [ProducesResponseType<PagedResult<RoundDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RoundDto>>> GetHistory(string slug, [FromQuery] RoundHistoryQuery query, CancellationToken ct) =>
        Ok(await _roundService.GetHistoryAsync(slug, query, ct));

    [AllowAnonymous]
    [HttpGet("api/rounds/{id:int}")]
    [ProducesResponseType<RoundDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoundDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _roundService.GetByIdAsync(id, ct));

    /// <summary>All bets of a round: who bet how much and when they cashed out.</summary>
    [AllowAnonymous]
    [HttpGet("api/rounds/{id:int}/bets")]
    [ProducesResponseType<IReadOnlyList<RoundBetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RoundBetDto>>> GetBets(int id, CancellationToken ct) =>
        Ok(await _roundService.GetBetsAsync(id, ct));

    /// <summary>Cancels an unfinished round; every active bet is refunded automatically.</summary>
    /// <remarks>Not possible once the round has crashed (409).</remarks>
    [HttpPost("api/rounds/{id:int}/cancel")]
    [Authorize(Roles = RoleNames.OperatorOrAdmin)]
    [ProducesResponseType<RoundCancellationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoundCancellationDto>> Cancel(int id, CancelRoundRequest request, CancellationToken ct) =>
        Ok(await _roundService.CancelAsync(id, request, ct));
}
