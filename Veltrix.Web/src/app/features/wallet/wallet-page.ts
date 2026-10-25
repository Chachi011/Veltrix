import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TransactionType } from '../../core/models/api.models';
import { WalletStore } from '../../core/state/wallet.store';
import { Pagination } from '../../shared/pagination';
import { VlxPipe } from '../../shared/pipes';

@Component({
  selector: 'vx-wallet-page',
  imports: [DatePipe, FormsModule, Pagination, VlxPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './wallet-page.html',
  styleUrl: './wallet-page.scss'
})
export class WalletPage {
  protected readonly store = inject(WalletStore);

  protected readonly types: TransactionType[] = ['Bet', 'Win', 'Refund', 'Bonus', 'AdminAdjustment'];

  /** Ticks every second for the "next bonus in ..." countdown. */
  private readonly now = signal(Date.now());

  protected readonly bonusCountdown = computed(() => {
    const next = this.store.wallet()?.nextDailyBonusAt;
    if (!next) {
      return null;
    }
    const ms = Date.parse(next) - this.now();
    if (ms <= 0) {
      return null;
    }
    const h = Math.floor(ms / 3_600_000);
    const m = Math.floor((ms % 3_600_000) / 60_000);
    const s = Math.floor((ms % 60_000) / 1000);
    return `${h}h ${m.toString().padStart(2, '0')}m ${s.toString().padStart(2, '0')}s`;
  });

  /** Claimable now, or the countdown has just reached zero. */
  protected readonly canClaim = computed(() => !!this.store.wallet() && this.bonusCountdown() === null);

  constructor() {
    void this.store.load().catch(() => undefined);
    void this.store.loadTransactions({ page: 1 }).catch(() => undefined);

    const timer = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected filterByType(type: TransactionType | null): void {
    void this.store.loadTransactions({ type, page: 1 });
  }

  protected goToPage(page: number): void {
    void this.store.loadTransactions({ page });
  }

  protected typeLabel(type: TransactionType): string {
    return type === 'AdminAdjustment' ? 'Adjustment' : type;
  }
}
