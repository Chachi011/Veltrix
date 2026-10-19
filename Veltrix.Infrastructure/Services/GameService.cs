using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Veltrix.Application.DTOs.Games;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services
{
    public class GameService : IGameService
    {
        private readonly ApplicationDbContext _context;

        public GameService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GameDto>> GetAllAsync()
        {
            return await _context.Games
                .AsNoTracking()
                .Select(game => new GameDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    Slug = game.Slug,
                    Description = game.Description,
                    Type = game.Type,
                    Status = game.Status,
                    MinBet = game.MinBet,
                    MaxBet = game.MaxBet,
                    RTP = game.RTP,
                    CreatedAt = game.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<GameDto?> GetByIdAsync(Guid id)
        {
            return await _context.Games
                .AsNoTracking()
                .Where(game => game.Id == id)
                .Select(game => new GameDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    Slug = game.Slug,
                    Description = game.Description,
                    Type = game.Type,
                    Status = game.Status,
                    MinBet = game.MinBet,
                    MaxBet = game.MaxBet,
                    RTP = game.RTP,
                    CreatedAt = game.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<GameDto> CreateAsync(CreateGameDto dto)
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Slug = dto.Slug,
                Description = dto.Description,
                Type = dto.Type,
                Status = GameStatus.Active,
                MinBet = dto.MinBet,
                MaxBet = dto.MaxBet,
                RTP = dto.RTP,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);

            await _context.SaveChangesAsync();

            return new GameDto
            {
                Id = game.Id,
                Name = game.Name,
                Slug = game.Slug,
                Description = game.Description,
                Type = game.Type,
                Status = game.Status,
                MinBet = game.MinBet,
                MaxBet = game.MaxBet,
                RTP = game.RTP,
                CreatedAt = game.CreatedAt
            };
        }
    }
}

