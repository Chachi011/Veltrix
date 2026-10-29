using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

/// <summary>User management. Admin only; every change is written to the audit log.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>Users table: search by email or nickname, filter by role and blocked state, paged (newest first).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUserListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AdminUserListItemDto>>> GetAll([FromQuery] AdminUserQuery query, CancellationToken ct) =>
        Ok(await _adminUserService.GetUsersAsync(query, ct));

    /// <summary>One user with balance, stats, limits, active sessions, recent transactions and adjustments.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminUserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailsDto>> Get(int id, CancellationToken ct) =>
        Ok(await _adminUserService.GetUserAsync(id, ct));

    /// <summary>Player / Operator / Admin. You cannot change your own role.</summary>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType<AdminUserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailsDto>> ChangeRole(int id, ChangeRoleRequest request, CancellationToken ct) =>
        Ok(await _adminUserService.ChangeRoleAsync(id, request, ct));

    /// <summary>
    /// Blocks ({"isBlocked": true}) or unblocks a user. Blocking signs the user out on every device
    /// and refuses their bets at once. You cannot block yourself.
    /// </summary>
    [HttpPut("{id:int}/block")]
    [ProducesResponseType<AdminUserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailsDto>> SetBlocked(int id, BlockUserRequest request, CancellationToken ct) =>
        Ok(await _adminUserService.SetBlockedAsync(id, request, ct));

    /// <summary>
    /// Adds (positive amount) or removes (negative amount) virtual VLX with a REQUIRED reason.
    /// The balance can never go negative, and you cannot adjust your own balance.
    /// </summary>
    [HttpPost("{id:int}/balance-adjustment")]
    [ProducesResponseType<BalanceAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BalanceAdjustmentDto>> AdjustBalance(int id, BalanceAdjustmentRequest request, CancellationToken ct) =>
        Ok(await _adminUserService.AdjustBalanceAsync(id, request, ct));
}
