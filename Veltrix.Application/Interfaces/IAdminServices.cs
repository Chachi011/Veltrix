using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Stats;

namespace Veltrix.Application.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUserQuery query, CancellationToken ct = default);
    Task<AdminUserDetailsDto> GetUserAsync(int id, CancellationToken ct = default);
    Task<AdminUserDetailsDto> ChangeRoleAsync(int id, ChangeRoleRequest request, CancellationToken ct = default);
    Task<AdminUserDetailsDto> SetBlockedAsync(int id, BlockUserRequest request, CancellationToken ct = default);
    Task<BalanceAdjustmentDto> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken ct = default);
}

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken ct = default);
}

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken ct = default);
    Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default);
}

public interface IAdminStatsService
{
    Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyStatisticDto>> GetDailyAsync(DailyStatsQuery query, CancellationToken ct = default);
}
