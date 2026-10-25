import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BetsApi } from '../../core/api/bets.api';
import { GamesApi } from '../../core/api/games.api';
import { BetDto, BetStatus, MyBetsQuery, PagedResult, RoundDto } from '../../core/models/api.models';
import { Pagination } from '../../shared/pagination';
import { MultiplierPipe, ShortHashPipe, VlxPipe } from '../../shared/pipes';

type Tab = 'bets' | 'rounds';

@Component({
  selector: 'vx-history-page',
  imports: [DatePipe, FormsModule, RouterLink, Pagination, VlxPipe, MultiplierPipe, ShortHashPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './history-page.html',
  styleUrl: './history-page.scss'
})
export class HistoryPage {
  private readonly betsApi = inject(BetsApi);
  private readonly gamesApi = inject(GamesApi);

  protected readonly tab = signal<Tab>('bets');
  protected readonly statuses: BetStatus[] = ['Active', 'CashedOut', 'Lost', 'Refunded'];

  // Filters as plain signals; dates come from <input type="date"> as yyyy-MM-dd
  protected readonly status = signal<BetStatus | null>(null);
  protected readonly from = signal('');
  protected readonly to = signal('');

  protected readonly bets = signal<PagedResult<BetDto> | null>(null);
  protected readonly rounds = signal<PagedResult<RoundDto> | null>(null);

  constructor() {
    void this.loadBets(1);
  }

  protected selectTab(tab: Tab): void {
    this.tab.set(tab);
    if (tab === 'rounds' && !this.rounds()) {
      void this.loadRounds(1);
    }
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
    this.bets.set(await firstValueFrom(this.betsApi.my(query)).catch(() => this.bets()));
  }

  protected async loadRounds(page: number): Promise<void> {
    this.rounds.set(await firstValueFrom(this.gamesApi.roundHistory('crash', page, 15)).catch(() => this.rounds()));
  }

  protected statusClass(status: BetStatus): string {
    return { Active: 'badge-primary', CashedOut: 'badge-success', Lost: 'badge-danger', Refunded: '' }[status];
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
}
