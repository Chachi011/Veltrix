using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/players")]
[AllowAnonymous]
[Produces("application/json")]
public class PlayersController : ControllerBase
{
    private readonly IProfileService _profileService;

    public PlayersController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>Public player profile: nickname, avatar, member since, level and best results. No private data.</summary>
    [HttpGet("{nickname}")]
    [ProducesResponseType<PublicProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicProfileDto>> GetByNickname(string nickname, CancellationToken ct) =>
        Ok(await _profileService.GetPublicProfileAsync(nickname, ct));
}
