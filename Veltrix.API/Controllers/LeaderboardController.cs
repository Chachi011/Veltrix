using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Leaderboard;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/leaderboard")]
[Produces("application/json")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Top players. period: day (last 24 h), week (last 7 days) or all. type: multiplier (best cash-out)
    /// or profit (payouts minus stakes). Open to guests; cached for 10 seconds.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<LeaderboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaderboardDto>> Get([FromQuery] LeaderboardQuery query, CancellationToken ct) =>
        Ok(await _leaderboardService.GetAsync(query, ct));
}
