using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Assistant;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Assistant;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// The players' AI assistant. This class decides WHAT the AI gets (the rules, the player's situation, the recent
/// conversation) and saves the conversation; IAssistantClient only carries it to n8n and back.
/// </summary>
public sealed class AssistantService : IAssistantService
{
    /// <summary>The chat panel shows the latest messages only.</summary>
    public const int HistoryPageSize = 50;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAssistantClient _client;
    private readonly ILimitsService _limits;
    private readonly IOptions<AssistantOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IAssistantClient client,
        ILimitsService limits,
        IOptions<AssistantOptions> options,
        TimeProvider time,
        ILogger<AssistantService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _client = client;
        _limits = limits;
        _options = options;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public AssistantStatusDto GetStatus() => new() { Configured = _client.IsConfigured };

    public async Task<IReadOnlyList<AssistantMessageDto>> GetMyHistoryAsync(CancellationToken ct = default)
    {
        var latest = await LatestAsync(CurrentUserId, HistoryPageSize, ct);
        return latest.Select(ToDto).ToList();
    }

    public async Task<AssistantReplyDto> AskAsync(AskAssistantRequest request, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var question = request.Message.Trim();
        var askedAt = Now;

        // The player's situation decides part of the rules (no invitation to play while betting is paused)
        var limits = await _limits.GetMyLimitsAsync(ct);
        var language = await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Language)
            .FirstOrDefaultAsync(ct) ?? UserPreference.DefaultLanguage;
        var nickname = await _db.Users.Where(u => u.Id == userId).Select(u => u.Nickname).FirstAsync(ct);

        // The recent conversation, so a follow-up question ("and how do I verify it?") makes sense to the AI
        var historySize = Math.Clamp(_options.Value.HistoryMessages, 0, 20);
        var history = (await LatestAsync(userId, historySize, ct))
            .Select(m => new AssistantTurn(m.Role == AssistantMessageRole.User ? "user" : "assistant", m.Text))
            .ToList();

        var answer = await _client.AskAsync(new AssistantRequest(
            SessionId: $"veltrix-user-{userId}",
            Language: language,
            SystemPrompt: AssistantPrompts.Build(language, nickname, limits.CanPlayNow, limits.IsSelfExcluded),
            History: history,
            Message: question), ct);

        // Saved only now: a question the AI could not answer is not stored, the client keeps it in the input box
        var questionRow = AssistantMessage.Question(userId, question, askedAt);
        var answerRow = AssistantMessage.Answer(userId, answer, Now);
        _db.AssistantMessages.AddRange(questionRow, answerRow);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Assistant answered user {UserId} ({Length} characters)", userId, answerRow.Text.Length);

        return new AssistantReplyDto { Question = ToDto(questionRow), Answer = ToDto(answerRow) };
    }

    public async Task<int> ClearMyHistoryAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        // Load and remove (not ExecuteDelete) so the in-memory test database can run it too; a chat is small
        var mine = await _db.AssistantMessages.Where(m => m.UserId == userId).ToListAsync(ct);
        _db.AssistantMessages.RemoveRange(mine);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} cleared their assistant chat ({Count} messages)", userId, mine.Count);
        return mine.Count;
    }

    /// <summary>The latest <paramref name="count"/> messages of a user, oldest first.</summary>
    private async Task<List<AssistantMessage>> LatestAsync(int userId, int count, CancellationToken ct)
    {
        if (count == 0)
            return new List<AssistantMessage>();

        var latest = await _db.AssistantMessages.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Take(count)
            .ToListAsync(ct);

        latest.Reverse();
        return latest;
    }

    private static AssistantMessageDto ToDto(AssistantMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Text = message.Text,
        CreatedAt = message.CreatedAt
    };
}
