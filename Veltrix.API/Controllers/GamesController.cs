using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Games;
using Veltrix.Application.Interfaces;


namespace Veltrix.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var games = await _gameService.GetAllAsync();

            return Ok(games);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var game = await _gameService.GetByIdAsync(id);

            if (game is null)
                return NotFound();

            return Ok(game);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGameDto dto)
        {
            var game = await _gameService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = game.Id },
                game);
        }
    }
}
