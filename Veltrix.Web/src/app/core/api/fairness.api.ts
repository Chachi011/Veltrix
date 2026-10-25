import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { FairnessRoundDto, VerifyFairnessRequest, VerifyFairnessResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FairnessApi {
  private readonly http = inject(HttpClient);

  round(roundId: number): Observable<FairnessRoundDto> {
    return this.http.get<FairnessRoundDto>(`/api/fairness/rounds/${roundId}`);
  }

  verify(request: VerifyFairnessRequest): Observable<VerifyFairnessResult> {
    return this.http.post<VerifyFairnessResult>('/api/fairness/verify', request);
  }
}
