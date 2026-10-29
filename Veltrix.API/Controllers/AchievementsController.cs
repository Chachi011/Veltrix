using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Achievements;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

/// <summary>Achievements (spec 4.6). They are unlocked automatically when a round is settled.</summary>
[ApiController]
[Route("api/achievements")]
[Produces("application/json")]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementService _achievements;

    public AchievementsController(IAchievementService achievements)
    {
        _achievements = achievements;
    }

    /// <summary>All achievements with their XP / VLX reward and how many players unlocked each. Open to guests.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<AchievementDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> GetAll(CancellationToken ct) =>
        Ok(await _achievements.GetAllAsync(ct));

    /// <summary>My achievements: unlocked ones with the date, the others with the progress so far (e.g. 37 / 100 bets).</summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<MyAchievementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MyAchievementDto>>> GetMine(CancellationToken ct) =>
        Ok(await _achievements.GetMineAsync(ct));
}
