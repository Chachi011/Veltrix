using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.DTOs.Wallet;

namespace Veltrix.Application.Interfaces;

public interface IRoundService
{
    Task<RoundDto> GetCurrentAsync(string gameSlug, CancellationToken ct = default);
    Task<PagedResult<RoundDto>> GetHistoryAsync(string gameSlug, RoundHistoryQuery query, CancellationToken ct = default);
    Task<RoundDto> GetByIdAsync(int roundId, CancellationToken ct = default);
    Task<IReadOnlyList<RoundBetDto>> GetBetsAsync(int roundId, CancellationToken ct = default);
    Task<RoundCancellationDto> CancelAsync(int roundId, CancelRoundRequest request, CancellationToken ct = default);
}

public interface IBetService
{
    Task<BetResultDto> PlaceBetAsync(PlaceBetRequest request, CancellationToken ct = default);
    Task<BetResultDto> CashOutAsync(int betId, CancellationToken ct = default);
    Task<PagedResult<BetDto>> GetMyBetsAsync(MyBetsQuery query, CancellationToken ct = default);
}

public interface IWalletService
{
    Task<WalletDto> GetMyWalletAsync(CancellationToken ct = default);
    Task<PagedResult<WalletTransactionDto>> GetMyTransactionsAsync(TransactionQuery query, CancellationToken ct = default);
    Task<WalletDto> ClaimDailyBonusAsync(CancellationToken ct = default);
}

public interface IFairnessService
{
    Task<FairnessRoundDto> GetRoundAsync(int roundId, CancellationToken ct = default);
    VerifyFairnessResult Verify(VerifyFairnessRequest request);
}
