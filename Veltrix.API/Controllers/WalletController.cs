using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
[Produces("application/json")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>My VLX balance and daily bonus status. Virtual points only, with no monetary value.</summary>
    [HttpGet]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletDto>> Get(CancellationToken ct) =>
        Ok(await _walletService.GetMyWalletAsync(ct));

    /// <summary>My ledger: every balance change with the balance before and after. Filter by type and date.</summary>
    [HttpGet("transactions")]
    [ProducesResponseType<PagedResult<WalletTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WalletTransactionDto>>> GetTransactions([FromQuery] TransactionQuery query, CancellationToken ct) =>
        Ok(await _walletService.GetMyTransactionsAsync(query, ct));

    /// <summary>Claims the daily bonus (once every 24 hours).</summary>
    [HttpPost("daily-bonus")]
    [ProducesResponseType<WalletDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletDto>> ClaimDailyBonus(CancellationToken ct) =>
        Ok(await _walletService.ClaimDailyBonusAsync(ct));
}
