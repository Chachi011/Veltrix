import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from '../http/http-context';
import { BetDto, BetResultDto, MyBetsQuery, PagedResult, PlaceBetRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BetsApi {
  private readonly http = inject(HttpClient);

  place(request: PlaceBetRequest): Observable<BetResultDto> {
    return this.http.post<BetResultDto>('/api/bets', request);
  }

  /** No body: the server decides the multiplier from its own clock. */
  cashOut(betId: number): Observable<BetResultDto> {
    return this.http.post<BetResultDto>(`/api/bets/${betId}/cashout`, null);
  }

  my(query: MyBetsQuery): Observable<PagedResult<BetDto>> {
    return this.http.get<PagedResult<BetDto>>('/api/bets/my', { params: toParams(query) });
  }
}
