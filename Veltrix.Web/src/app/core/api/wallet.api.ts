import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toParams } from '../http/http-context';
import { PagedResult, TransactionQuery, WalletDto, WalletTransactionDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class WalletApi {
  private readonly http = inject(HttpClient);

  get(): Observable<WalletDto> {
    return this.http.get<WalletDto>('/api/wallet');
  }

  transactions(query: TransactionQuery): Observable<PagedResult<WalletTransactionDto>> {
    return this.http.get<PagedResult<WalletTransactionDto>>('/api/wallet/transactions', { params: toParams(query) });
  }

  claimDailyBonus(): Observable<WalletDto> {
    return this.http.post<WalletDto>('/api/wallet/daily-bonus', null);
  }
}
