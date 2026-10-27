import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_LOADING, toParams } from '../http/http-context';
import { LeaderboardDto, LeaderboardPeriod, LeaderboardType, MyStatsDto } from '../models/api.models';

/** Public leaderboard and personal statistics. */
@Injectable({ providedIn: 'root' })
export class StatsApi {
  private readonly http = inject(HttpClient);

  leaderboard(period: LeaderboardPeriod, type: LeaderboardType, limit = 20): Observable<LeaderboardDto> {
    return this.http.get<LeaderboardDto>('/api/leaderboard', { params: toParams({ period, type, limit }) });
  }

  myStats(days: number): Observable<MyStatsDto> {
    return this.http.get<MyStatsDto>('/api/stats/me', {
      params: toParams({ days }),
      context: new HttpContext().set(SKIP_LOADING, true)
    });
  }
}
