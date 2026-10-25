import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { TransactionType } from '../../core/models/api.models';
import { WalletStore } from '../../core/state/wallet.store';
import { VlxPipe } from '../../shared/pipes';
import {
  AnimatedNumber,
  Badge,
  BadgeTone,
  Button,
  Card,
  EmptyState,
  Icons,
  PageHeader,
  Pagination,
  ProgressRing,
  Skeleton
} from '../../shared/ui';

const DAY_MS = 24 * 60 * 60 * 1000;

@Component({
  selector: 'vx-wallet-page',
  imports: [
    DatePipe,
    FormsModule,
    LucideAngularModule,
    VlxPipe,
    AnimatedNumber,
    Badge,
    Button,
    Card,
    EmptyState,
    PageHeader,
    Pagination,
    ProgressRing,
    Skeleton
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './wallet-page.html'
})
export class WalletPage {
  protected readonly store = inject(WalletStore);
  protected readonly icons = Icons;

  protected readonly types: TransactionType[] = ['Bet', 'Win', 'Refund', 'Bonus', 'AdminAdjustment'];
  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  /** Ticks every second for the "next bonus in ..." countdown. */
  private readonly now = signal(Date.now());

  private readonly msUntilBonus = computed(() => {
    const next = this.store.wallet()?.nextDailyBonusAt;
    return next ? Math.max(0, Date.parse(next) - this.now()) : 0;
  });

  protected readonly canClaim = computed(() => !!this.store.wallet() && this.msUntilBonus() === 0);

  /** How much of the 24-hour wait has passed (0 … 1), for the ring. */
  protected readonly bonusProgress = computed(() => 1 - this.msUntilBonus() / DAY_MS);

  protected readonly bonusCountdown = computed(() => {
    const ms = this.msUntilBonus();
    const h = Math.floor(ms / 3_600_000);
    const m = Math.floor((ms % 3_600_000) / 60_000);
    const s = Math.floor((ms % 60_000) / 1000);
    return `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

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

  protected typeTone(type: TransactionType): BadgeTone {
    const tones: Record<TransactionType, BadgeTone> = {
      Bet: 'neutral',
      Win: 'success',
      Refund: 'accent',
      Bonus: 'primary',
      AdminAdjustment: 'warning'
    };
    return tones[type];
  }
}
