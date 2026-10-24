using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class WalletService : IWalletService
{
    private const decimal DefaultDailyBonus = 100m;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<WalletService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<WalletDto> GetMyWalletAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct)
            ?? throw new NotFoundException("Wallet not found.");

        var bonus = await SettingsReader.GetDecimalAsync(_db, SettingKeys.DailyBonusAmount, DefaultDailyBonus, ct);

        return new WalletDto
        {
            Balance = wallet.Balance,
            DailyBonusAmount = bonus,
            CanClaimDailyBonus = wallet.CanClaimDailyBonus(_time.GetUtcNow().UtcDateTime),
            LastDailyBonusAt = wallet.LastDailyBonusAt,
            NextDailyBonusAt = wallet.NextDailyBonusAt
        };
    }

    public async Task<PagedResult<WalletTransactionDto>> GetMyTransactionsAsync(TransactionQuery query, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var transactions = _db.WalletTransactions.AsNoTracking().Where(t => t.Wallet!.UserId == userId);

        if (query.Type.HasValue)
            transactions = transactions.Where(t => t.Type == query.Type.Value);
        if (query.From.HasValue)
            transactions = transactions.Where(t => t.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            transactions = transactions.Where(t => t.CreatedAt <= query.To.Value);

        var total = await transactions.CountAsync(ct);
        var items = await transactions
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                BetId = t.BetId,
                Reason = t.Reason,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<WalletTransactionDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<WalletDto> ClaimDailyBonusAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var amount = await SettingsReader.GetDecimalAsync(_db, SettingKeys.DailyBonusAmount, DefaultDailyBonus, ct);

        var balance = await ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);

            // Domain rule: once per 24 hours (throws with the next available time otherwise)
            wallet.ClaimDailyBonus(amount, _time.GetUtcNow().UtcDateTime);
            await _db.SaveChangesAsync(ct);
            return wallet.Balance;
        });

        _logger.LogInformation("User {UserId} claimed daily bonus {Amount}; balance {Balance}", userId, amount, balance);
        await _notifier.BalanceUpdatedAsync(userId, balance);

        return await GetMyWalletAsync(ct);
    }
}
