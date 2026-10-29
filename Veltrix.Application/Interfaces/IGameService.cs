using Veltrix.Application.DTOs.Games;

namespace Veltrix.Application.Interfaces;

public interface IGameService
{
    Task<IReadOnlyList<GameDto>> GetAllAsync(GameQuery query, CancellationToken ct = default);
    Task<GameDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<GameDto> CreateAsync(SaveGameRequest request, CancellationToken ct = default);
    Task<GameDto> UpdateAsync(int id, SaveGameRequest request, CancellationToken ct = default);
    Task<GameDto> UpdateStatusAsync(int id, UpdateGameStatusRequest request, CancellationToken ct = default);
}
