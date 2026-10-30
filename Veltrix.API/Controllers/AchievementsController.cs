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

    /// <summary>The active achievements (safety and learning first) with their XP / VLX reward and how many players unlocked each. Open to guests.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<AchievementDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> GetAll(CancellationToken ct) =>
        Ok(await _achievements.GetAllAsync(ct));

    /// <summary>
    /// My achievements: unlocked ones with the date, the others with their progress. While I cannot bet (self-excluded,
    /// or today's limit is used up) the ones that ask me to play are left out and playTasksHidden is true.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType<MyAchievementsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MyAchievementsDto>> GetMine(CancellationToken ct) =>
        Ok(await _achievements.GetMineAsync(ct));
}
