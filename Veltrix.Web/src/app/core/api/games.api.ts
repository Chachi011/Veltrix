import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SILENT_ERRORS, toParams } from '../http/http-context';
import {
  GameDto,
  GameQuery,
  GameStatus,
  PagedResult,
  RoundBetDto,
  RoundCancellationDto,
  RoundDto,
  SaveGameRequest
} from '../models/api.models';

/** Games and their rounds (reading is public; changes need the Operator or Admin role). */
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

  // ---------- Catalog (everyone) and operator (staff) ----------

  /** The catalog: search by name, filter by type and status. Staff also see Disabled games. */
  list(query: GameQuery = {}): Observable<GameDto[]> {
    return this.http.get<GameDto[]>('/api/games', { params: toParams(query) });
  }

  create(request: SaveGameRequest): Observable<GameDto> {
    return this.http.post<GameDto>('/api/games', request);
  }

  update(id: number, request: SaveGameRequest): Observable<GameDto> {
    return this.http.put<GameDto>(`/api/games/${id}`, request);
  }

  setStatus(id: number, status: GameStatus): Observable<GameDto> {
    return this.http.patch<GameDto>(`/api/games/${id}/status`, { status });
  }

  /** Every active bet of the round is refunded by the server. */
  cancelRound(roundId: number, reason: string): Observable<RoundCancellationDto> {
    return this.http.post<RoundCancellationDto>(`/api/rounds/${roundId}/cancel`, { reason });
  }
}
