import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from '../http/http-context';
import {
  AdminOverviewDto,
  AdminUserDetailsDto,
  AdminUserListItemDto,
  AdminUserQuery,
  AuditLogDto,
  AuditLogQuery,
  BalanceAdjustmentDto,
  BalanceAdjustmentRequest,
  DailyStatisticDto,
  PagedResult,
  SystemSettingsDto,
  UserRole
} from '../models/api.models';

/** Admin panel. The server checks the Admin role on every call; the route guard only hides the pages. */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  users(query: AdminUserQuery): Observable<PagedResult<AdminUserListItemDto>> {
    return this.http.get<PagedResult<AdminUserListItemDto>>('/api/admin/users', { params: toParams(query) });
  }

  user(id: number): Observable<AdminUserDetailsDto> {
    return this.http.get<AdminUserDetailsDto>(`/api/admin/users/${id}`);
  }

  changeRole(id: number, role: UserRole): Observable<AdminUserDetailsDto> {
    return this.http.put<AdminUserDetailsDto>(`/api/admin/users/${id}/role`, { role });
  }

  setBlocked(id: number, isBlocked: boolean, reason: string | null): Observable<AdminUserDetailsDto> {
    return this.http.put<AdminUserDetailsDto>(`/api/admin/users/${id}/block`, { isBlocked, reason });
  }

  adjustBalance(id: number, request: BalanceAdjustmentRequest): Observable<BalanceAdjustmentDto> {
    return this.http.post<BalanceAdjustmentDto>(`/api/admin/users/${id}/balance-adjustment`, request);
  }

  auditLogs(query: AuditLogQuery): Observable<PagedResult<AuditLogDto>> {
    return this.http.get<PagedResult<AuditLogDto>>('/api/admin/audit-logs', { params: toParams(query) });
  }

  auditActions(): Observable<string[]> {
    return this.http.get<string[]>('/api/admin/audit-logs/actions');
  }

  settings(): Observable<SystemSettingsDto> {
    return this.http.get<SystemSettingsDto>('/api/admin/settings');
  }

  updateSettings(startingBalance: number, dailyBonusAmount: number): Observable<SystemSettingsDto> {
    return this.http.put<SystemSettingsDto>('/api/admin/settings', { startingBalance, dailyBonusAmount });
  }

  overview(): Observable<AdminOverviewDto> {
    return this.http.get<AdminOverviewDto>('/api/admin/stats/overview');
  }

  daily(from: string, to: string): Observable<DailyStatisticDto[]> {
    return this.http.get<DailyStatisticDto[]>('/api/admin/stats/daily', { params: toParams({ from, to }) });
  }
}
