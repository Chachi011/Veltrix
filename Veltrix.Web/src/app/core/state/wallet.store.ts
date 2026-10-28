import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { WalletApi } from '../api/wallet.api';
import { PagedResult, TransactionQuery, WalletDto, WalletTransactionDto } from '../models/api.models';
import { I18n } from '../i18n/i18n';
import { ToastStore } from '../ui/toast.store';

type WalletState = {
  /** Shown in the header. Updated by bets, cash-outs and the BalanceUpdated SignalR event. */
  balance: number | null;
  wallet: WalletDto | null;
  transactions: PagedResult<WalletTransactionDto> | null;
  query: TransactionQuery;
  loadingTransactions: boolean;
  claiming: boolean;
};

const initialState: WalletState = {
  balance: null,
  wallet: null,
  transactions: null,
  query: { page: 1, pageSize: 10, type: null },
  loadingTransactions: false,
  claiming: false
};

export const WalletStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, api = inject(WalletApi), toasts = inject(ToastStore), i18n = inject(I18n)) => ({
    async load(): Promise<void> {
      const wallet = await firstValueFrom(api.get());
      patchState(store, { wallet, balance: wallet.balance });
    },

    /** The server already knows the new balance (bet / cash-out response, SignalR); no extra request needed. */
    setBalance(balance: number): void {
      patchState(store, { balance });
    },

    async claimDailyBonus(): Promise<void> {
      patchState(store, { claiming: true });
      try {
        const wallet = await firstValueFrom(api.claimDailyBonus());
        patchState(store, { wallet, balance: wallet.balance });
        toasts.success(i18n.t('Daily bonus claimed: +{amount} VLX', { amount: wallet.dailyBonusAmount }));
      } catch {
        // The error interceptor already showed the reason (e.g. "available again at ...")
      } finally {
        patchState(store, { claiming: false });
      }
    },

    async loadTransactions(changes: Partial<TransactionQuery> = {}): Promise<void> {
      const query = { ...store.query(), ...changes };
      patchState(store, { query, loadingTransactions: true });
      try {
        patchState(store, { transactions: await firstValueFrom(api.transactions(query)) });
      } finally {
        patchState(store, { loadingTransactions: false });
      }
    },

    /** On logout: forget everything about the previous user. */
    reset(): void {
      patchState(store, initialState);
    }
  }))
);
