using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>The logged-in user's profile and VLX balance.</summary>
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct) =>
        Ok(await _profileService.GetMyProfileAsync(ct));
}
