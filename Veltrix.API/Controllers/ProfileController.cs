using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Veltrix.API.Extensions;
using Veltrix.API.Models;
using Veltrix.Application.DTOs.Profile;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    // A little above 2 MB so the multipart envelope fits; the validator enforces the exact 2 MB file limit
    private const long MaxAvatarRequestBytes = 3 * 1024 * 1024;

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

    /// <summary>Changes the nickname (3-20 letters, digits or underscore; must be unique).</summary>
    [HttpPut]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _profileService.UpdateMyProfileAsync(request, ct));

    /// <summary>Changes the password. Requires the current password; logs out all other sessions.</summary>
    /// <remarks>
    /// All refresh tokens are revoked. The access token already in use stays valid until it
    /// expires (max 15 minutes), because JWTs are verified without a DB lookup.
    /// </remarks>
    [HttpPut("password")]
    [EnableRateLimiting(ApiServiceExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _profileService.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>Uploads a new avatar (multipart/form-data, field "file"). JPG, PNG or WEBP, max 2 MB.</summary>
    /// <remarks>Requests over 3 MB are rejected while the form is read, also as 400 ProblemDetails.</remarks>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAvatarRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAvatarRequestBytes)]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileDto>> UploadAvatar([FromForm] AvatarUploadForm form, CancellationToken ct)
    {
        // The validator has already checked File is present and <= 2 MB, so reading it into memory is safe
        using var buffer = new MemoryStream((int)form.File!.Length);
        await form.File.CopyToAsync(buffer, ct);

        return Ok(await _profileService.UploadAvatarAsync(buffer.ToArray(), ct));
    }
}
