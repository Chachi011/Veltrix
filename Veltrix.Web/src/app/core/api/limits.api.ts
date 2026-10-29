import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PlayerLimitsDto, SelfExclusionPeriod, UpdateLimitsRequest } from '../models/api.models';

/** Responsible play tools (Player only). */
@Injectable({ providedIn: 'root' })
export class LimitsApi {
  private readonly http = inject(HttpClient);

  get(): Observable<PlayerLimitsDto> {
    return this.http.get<PlayerLimitsDto>('/api/limits');
  }

  update(request: UpdateLimitsRequest): Observable<PlayerLimitsDto> {
    return this.http.put<PlayerLimitsDto>('/api/limits', request);
  }

  selfExclude(period: SelfExclusionPeriod): Observable<PlayerLimitsDto> {
    return this.http.post<PlayerLimitsDto>('/api/limits/self-exclusion', { period });
  }
}
