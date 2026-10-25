import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { BetsApi } from '../../core/api/bets.api';
import { GamesApi } from '../../core/api/games.api';
import { BetDto, BetStatus, MyBetsQuery, PagedResult, RoundDto } from '../../core/models/api.models';
import { MultiplierPipe, VlxPipe } from '../../shared/pipes';
import {
  Badge,
  BadgeTone,
  Button,
  Card,
  CopyButton,
  EmptyState,
  Icons,
  MultiplierPill,
  PageHeader,
  Pagination,
  Skeleton,
  TabItem,
  Tabs
} from '../../shared/ui';

@Component({
  selector: 'vx-history-page',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    LucideAngularModule,
    MultiplierPipe,
    VlxPipe,
    Badge,
    Button,
    Card,
    CopyButton,
    EmptyState,
    MultiplierPill,
    PageHeader,
    Pagination,
    Skeleton,
    Tabs
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './history-page.html'
})
export class HistoryPage {
  private readonly betsApi = inject(BetsApi);
  private readonly gamesApi = inject(GamesApi);

  protected readonly icons = Icons;
  protected readonly tabs: TabItem[] = [
    { value: 'bets', label: 'My bets' },
    { value: 'rounds', label: 'All rounds' }
  ];
  protected readonly tab = signal('bets');

  protected readonly statuses: BetStatus[] = ['Active', 'CashedOut', 'Lost', 'Refunded'];
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];

  // Filters as plain signals; dates come from <input type="date"> as yyyy-MM-dd
  protected readonly status = signal<BetStatus | null>(null);
  protected readonly from = signal('');
  protected readonly to = signal('');

  protected readonly bets = signal<PagedResult<BetDto> | null>(null);
  protected readonly rounds = signal<PagedResult<RoundDto> | null>(null);
  protected readonly loading = signal(false);

  constructor() {
    void this.loadBets(1);

    // The rounds tab loads on first visit
    effect(() => {
      if (this.tab() === 'rounds') {
        untracked(() => {
          if (!this.rounds()) {
            void this.loadRounds(1);
          }
        });
      }
    });
  }

  protected applyFilters(): void {
    void this.loadBets(1);
  }

  protected clearFilters(): void {
    this.status.set(null);
    this.from.set('');
    this.to.set('');
    void this.loadBets(1);
  }

  protected async loadBets(page: number): Promise<void> {
    const query: MyBetsQuery = {
      page,
      pageSize: 15,
      status: this.status(),
      // Whole days in UTC: from 00:00:00 of the first day to 23:59:59 of the last
      from: this.from() ? `${this.from()}T00:00:00Z` : null,
      to: this.to() ? `${this.to()}T23:59:59Z` : null
    };
    this.loading.set(true);
    try {
      this.bets.set(await firstValueFrom(this.betsApi.my(query)).catch(() => this.bets()));
    } finally {
      this.loading.set(false);
    }
  }

  protected async loadRounds(page: number): Promise<void> {
    this.loading.set(true);
    try {
      this.rounds.set(await firstValueFrom(this.gamesApi.roundHistory('crash', page, 15)).catch(() => this.rounds()));
    } finally {
      this.loading.set(false);
    }
  }

  protected statusTone(status: BetStatus): BadgeTone {
    const tones: Record<BetStatus, BadgeTone> = { Active: 'primary', CashedOut: 'success', Lost: 'danger', Refunded: 'neutral' };
    return tones[status];
  }

  protected statusLabel(status: BetStatus): string {
    return status === 'CashedOut' ? 'Cashed out' : status;
  }

  /** Profit of a settled bet: payout − stake (a refund is 0). */
  protected profit(bet: BetDto): number | null {
    if (bet.status === 'Active') return null;
    if (bet.status === 'Refunded') return 0;
    return (bet.payout ?? 0) - bet.amount;
  }

  protected shortHash(hash: string): string {
    return `${hash.slice(0, 10)}…${hash.slice(-4)}`;
  }
}
