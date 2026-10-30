using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Content;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/pages")]
[Produces("application/json")]
public class PagesController : ControllerBase
{
    private readonly IStaticPageService _pageService;
    private readonly IAchievementService _achievements;

    public PagesController(IStaticPageService pageService, IAchievementService achievements)
    {
        _pageService = pageService;
        _achievements = achievements;
    }

    /// <summary>
    /// A content page, open to guests: "rules" (how Crash works) or "no-winning-strategy".
    /// lang = en (default); a missing translation falls back to English.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<StaticPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaticPageDto>> Get(string slug, [FromQuery] string? lang, CancellationToken ct) =>
        Ok(await _pageService.GetAsync(slug, lang, ct));

    /// <summary>
    /// The player read this page to its end (the Rules page sends it for "no-winning-strategy"). Reading the rules
    /// completes the learning achievement "Know the rules" once; other pages are only checked to exist.
    /// </summary>
    [HttpPost("{slug}/read")]
    [Authorize(Roles = RoleNames.Player)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(string slug, CancellationToken ct)
    {
        await _achievements.RecordPageReadAsync(slug, ct);
        return NoContent();
    }
}
