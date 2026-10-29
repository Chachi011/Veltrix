using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Content;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/pages")]
[Produces("application/json")]
public class PagesController : ControllerBase
{
    private readonly IStaticPageService _pageService;

    public PagesController(IStaticPageService pageService)
    {
        _pageService = pageService;
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
}
