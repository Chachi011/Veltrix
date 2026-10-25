using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Games;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class GameService : IGameService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<GameService> _logger;

    public GameService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        TimeProvider time,
        ILogger<GameService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    // Translated by EF Core into the SQL SELECT list, so only needed columns are read
    private static readonly Expression<Func<Game, GameDto>> ToDto = game => new GameDto
    {
        Id = game.Id,
        Name = game.Name,
        Slug = game.Slug,
        Description = game.Description,
        Type = game.Type,
        Status = game.Status,
        MinBet = game.MinBet,
        MaxBet = game.MaxBet,
        BettingDurationSec = game.BettingDurationSec,
        PauseBetweenRoundsSec = game.PauseBetweenRoundsSec,
        CreatedAt = game.CreatedAt,
        UpdatedAt = game.UpdatedAt
    };

    // Same mapping as plain C#, for entities already in memory
    private static readonly Func<Game, GameDto> MapToDto = ToDto.Compile();

    public async Task<IReadOnlyList<GameDto>> GetAllAsync(GameQuery query, CancellationToken ct = default)
    {
        var games = VisibleGames();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            games = games.Where(g => g.Name.Contains(search) || g.Slug.Contains(search));
        }

        if (query.Type.HasValue)
            games = games.Where(g => g.Type == query.Type.Value);

        if (query.Status.HasValue)
            games = games.Where(g => g.Status == query.Status.Value);

        return await games.OrderBy(g => g.Name).Select(ToDto).ToListAsync(ct);
    }

    public async Task<GameDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await VisibleGames()
            .Where(g => g.Slug == slug)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Game '{slug}' was not found.");
    }

    public async Task<GameDto> CreateAsync(SaveGameRequest request, CancellationToken ct = default)
    {
        await EnsureSlugIsFreeAsync(request.Slug, exceptGameId: null, ct);

        var now = _time.GetUtcNow().UtcDateTime;
        var game = new Game
        {
            Type = GameType.Crash,
            Status = GameStatus.Active,
            CreatedAt = now
        };
        Apply(request, game);

        _db.Games.Add(game);
        await _db.SaveChangesAsync(ct);

        // Second save: the audit row needs the generated game.Id
        _db.AuditLogs.Add(AuditTrail.Entry(_currentUser.UserId, AuditActions.GameCreated, nameof(Game), game.Id.ToString(), null, MapToDto(game), now));
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Game {GameId} ({Slug}) created by user {UserId}", game.Id, game.Slug, _currentUser.UserId);
        return MapToDto(game);
    }

    public async Task<GameDto> UpdateAsync(int id, SaveGameRequest request, CancellationToken ct = default)
    {
        var game = await FindAsync(id, ct);
        await EnsureSlugIsFreeAsync(request.Slug, exceptGameId: id, ct);

        var now = _time.GetUtcNow().UtcDateTime;
        var before = MapToDto(game);

        Apply(request, game);
        game.UpdatedAt = now;

        var after = MapToDto(game);
        _db.AuditLogs.Add(AuditTrail.Entry(_currentUser.UserId, AuditActions.GameUpdated, nameof(Game), game.Id.ToString(), before, after, now));
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Game {GameId} updated by user {UserId}", game.Id, _currentUser.UserId);
        return after;
    }

    public async Task<GameDto> UpdateStatusAsync(int id, UpdateGameStatusRequest request, CancellationToken ct = default)
    {
        var game = await FindAsync(id, ct);
        var now = _time.GetUtcNow().UtcDateTime;
        var oldStatus = game.Status;

        game.Status = request.Status;
        game.UpdatedAt = now;

        _db.AuditLogs.Add(AuditTrail.Entry(
            _currentUser.UserId, AuditActions.GameStatusChanged, nameof(Game), game.Id.ToString(),
            new { Status = oldStatus.ToString() }, new { Status = game.Status.ToString() }, now));
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Game {GameId} status {OldStatus} -> {NewStatus} by user {UserId}", game.Id, oldStatus, game.Status, _currentUser.UserId);
        return MapToDto(game);
    }

    /// <summary>Guests and players never see Disabled games; staff see everything.</summary>
    private IQueryable<Game> VisibleGames()
    {
        var games = _db.Games.AsNoTracking();
        return _currentUser.IsStaff ? games : games.Where(g => g.Status != GameStatus.Disabled);
    }

    private async Task<Game> FindAsync(int id, CancellationToken ct) =>
        await _db.Games.FirstOrDefaultAsync(g => g.Id == id, ct)
        ?? throw new NotFoundException($"Game {id} was not found.");

    private async Task EnsureSlugIsFreeAsync(string slug, int? exceptGameId, CancellationToken ct)
    {
        var taken = await _db.Games.AnyAsync(g => g.Slug == slug && g.Id != exceptGameId, ct);
        if (taken)
            throw new ConflictException($"Slug '{slug}' is already used by another game.");
    }

    private static void Apply(SaveGameRequest request, Game game)
    {
        game.Name = request.Name.Trim();
        game.Slug = request.Slug.Trim();
        game.Description = request.Description?.Trim();
        game.MinBet = request.MinBet;
        game.MaxBet = request.MaxBet;
        game.BettingDurationSec = request.BettingDurationSec;
        game.PauseBetweenRoundsSec = request.PauseBetweenRoundsSec;
    }
}
