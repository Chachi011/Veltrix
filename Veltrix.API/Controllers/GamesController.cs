using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Games;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>Game catalog with search and filters. Open to guests; Disabled games are visible to staff only.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<GameDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetAll([FromQuery] GameQuery query, CancellationToken ct) =>
        Ok(await _gameService.GetAllAsync(query, ct));

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await _gameService.GetBySlugAsync(slug, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.OperatorOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDto>> Create(SaveGameRequest request, CancellationToken ct)
    {
        var game = await _gameService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = game.Slug }, game);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.OperatorOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameDto>> Update(int id, SaveGameRequest request, CancellationToken ct) =>
        Ok(await _gameService.UpdateAsync(id, request, ct));

    /// <summary>Active / Maintenance / Disabled.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = RoleNames.OperatorOrAdmin)]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> UpdateStatus(int id, UpdateGameStatusRequest request, CancellationToken ct) =>
        Ok(await _gameService.UpdateStatusAsync(id, request, ct));
}
