using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Veltrix.API.Extensions;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/bets")]
[Authorize(Roles = RoleNames.Player)]
[Produces("application/json")]
public class BetsController : ControllerBase
{
    private readonly IBetService _betService;

    public BetsController(IBetService betService)
    {
        _betService = betService;
    }

    /// <summary>Places a bet on the current round of a game. Only while betting is open.</summary>
    [HttpPost]
    [EnableRateLimiting(ApiServiceExtensions.BetsRateLimitPolicy)]
    [ProducesResponseType<BetResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BetResultDto>> Place(PlaceBetRequest request, CancellationToken ct) =>
        Ok(await _betService.PlaceBetAsync(request, ct));

    /// <summary>Cashes out at the SERVER's current multiplier. The client sends no multiplier or amount.</summary>
    [HttpPost("{id:int}/cashout")]
    [EnableRateLimiting(ApiServiceExtensions.BetsRateLimitPolicy)]
    [ProducesResponseType<BetResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BetResultDto>> CashOut(int id, CancellationToken ct) =>
        Ok(await _betService.CashOutAsync(id, ct));

    /// <summary>My bet history with date/status filters and paging.</summary>
    [HttpGet("my")]
    [ProducesResponseType<PagedResult<BetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BetDto>>> GetMine([FromQuery] MyBetsQuery query, CancellationToken ct) =>
        Ok(await _betService.GetMyBetsAsync(query, ct));
}
