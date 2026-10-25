using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Stats;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminStatsController : ControllerBase
{
    private readonly IAdminStatsService _statsService;

    public AdminStatsController(IAdminStatsService statsService)
    {
        _statsService = statsService;
    }

    /// <summary>
    /// Dashboard header: users, online now, today's rounds/bets/volume (UTC day), VLX in circulation,
    /// and the crash point distribution of the last 1000 rounds next to the share the formula predicts.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType<AdminOverviewDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview(CancellationToken ct) =>
        Ok(await _statsService.GetOverviewAsync(ct));

    /// <summary>One row per day for the charts (from/to as yyyy-MM-dd, UTC; default the last 30 days, max 366).</summary>
    [HttpGet("daily")]
    [ProducesResponseType<IReadOnlyList<DailyStatisticDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DailyStatisticDto>>> GetDaily([FromQuery] DailyStatsQuery query, CancellationToken ct) =>
        Ok(await _statsService.GetDailyAsync(query, ct));
}
