using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Application.DTOs.Games;

namespace Veltrix.Application.Interfaces
{
    public interface IGameService
    {
        Task<IEnumerable<GameDto>> GetAllAsync();
        Task<GameDto?> GetByIdAsync(Guid id);
        Task<GameDto> CreateAsync(CreateGameDto dto);
    }
}
