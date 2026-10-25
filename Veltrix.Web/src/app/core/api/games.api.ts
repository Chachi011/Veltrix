import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SILENT_ERRORS, toParams } from '../http/http-context';
import { GameDto, PagedResult, RoundBetDto, RoundDto } from '../models/api.models';

/** Games and their rounds (all public). */
@Injectable({ providedIn: 'root' })
export class GamesApi {
  private readonly http = inject(HttpClient);

  getGame(slug: string): Observable<GameDto> {
    return this.http.get<GameDto>(`/api/games/${encodeURIComponent(slug)}`);
  }

  /** 404 simply means "no round played yet", so the caller handles it quietly. */
  currentRound(slug: string): Observable<RoundDto> {
    return this.http.get<RoundDto>(`/api/games/${encodeURIComponent(slug)}/rounds/current`, {
      context: new HttpContext().set(SILENT_ERRORS, true)
    });
  }

  roundHistory(slug: string, page: number, pageSize: number): Observable<PagedResult<RoundDto>> {
    return this.http.get<PagedResult<RoundDto>>(`/api/games/${encodeURIComponent(slug)}/rounds`, {
      params: toParams({ page, pageSize })
    });
  }

  roundBets(roundId: number): Observable<RoundBetDto[]> {
    return this.http.get<RoundBetDto[]>(`/api/rounds/${roundId}/bets`);
  }
}
