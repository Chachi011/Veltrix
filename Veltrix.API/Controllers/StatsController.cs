using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Stats;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    /// <summary>
    /// My statistics: bets, win rate, average cash-out, profit, a daily series for the chart (days = 7-90)
    /// and how my cash-outs are spread over multiplier ranges.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<MyStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyStatsDto>> GetMine([FromQuery] MyStatsQuery query, CancellationToken ct) =>
        Ok(await _statsService.GetMyStatsAsync(query, ct));
}
