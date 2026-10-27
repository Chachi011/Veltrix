import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { StatsApi } from '../../core/api/stats.api';
import { AuthStore } from '../../core/auth/auth.store';
import { LeaderboardDto, LeaderboardEntryDto, LeaderboardPeriod, LeaderboardType } from '../../core/models/api.models';
import { MultiplierPipe, VlxPipe } from '../../shared/pipes';
import {
  Avatar,
  Badge,
  Button,
  Card,
  EmptyState,
  Icons,
  MultiplierPill,
  PageHeader,
  Skeleton,
  TabItem,
  Tabs
} from '../../shared/ui';

const PERIODS: TabItem[] = [
  { value: 'Day', label: '24 hours' },
  { value: 'Week', label: '7 days' },
  { value: 'All', label: 'All time' }
];

const TYPES: TabItem[] = [
  { value: 'Multiplier', label: 'Best multiplier' },
  { value: 'Profit', label: 'Profit' }
];

/** Podium order on wide screens: 2nd, 1st, 3rd (the winner in the middle). */
const PODIUM_ORDER = [1, 0, 2];

@Component({
  selector: 'vx-leaderboard-page',
  imports: [
    RouterLink,
    LucideAngularModule,
    MultiplierPipe,
    VlxPipe,
    Avatar,
    Badge,
    Button,
    Card,
    EmptyState,
    MultiplierPill,
    PageHeader,
    Skeleton,
    Tabs
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leaderboard-page.html'
})
export class LeaderboardPage {
  private readonly api = inject(StatsApi);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthStore);

  protected readonly icons = Icons;
  protected readonly periods = PERIODS;
  protected readonly types = TYPES;
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];

  // The tabs live in the URL (?period=Week&type=Profit), so a board can be shared or bookmarked
  private readonly query = inject(ActivatedRoute).snapshot.queryParamMap;
  protected readonly period = signal<string>(pick(this.query.get('period'), PERIODS, 'Day'));
  protected readonly type = signal<string>(pick(this.query.get('type'), TYPES, 'Multiplier'));

  protected readonly board = signal<LeaderboardDto | null>(null);
  protected readonly loading = signal(false);

  protected readonly isMultiplier = computed(() => this.type() === 'Multiplier');
  protected readonly entries = computed(() => this.board()?.entries ?? []);
  protected readonly podium = computed(() =>
    PODIUM_ORDER.map((i) => this.entries()[i]).filter((e): e is LeaderboardEntryDto => e !== undefined)
  );
  /** The table lists everyone after the podium. */
  protected readonly rest = computed(() => this.entries().slice(3));
  protected readonly myRank = computed(() => {
    const nickname = this.auth.user()?.nickname;
    return nickname ? (this.entries().find((e) => e.nickname === nickname)?.rank ?? null) : null;
  });

  constructor() {
    effect(() => {
      const period = this.period() as LeaderboardPeriod;
      const type = this.type() as LeaderboardType;
      untracked(() => {
        void this.router.navigate([], { queryParams: { period, type }, replaceUrl: true });
        void this.load(period, type);
      });
    });
  }

  protected isMe(entry: LeaderboardEntryDto): boolean {
    return entry.nickname === this.auth.user()?.nickname;
  }

  /** Complete class strings per podium place (Tailwind generates only classes it finds literally). */
  protected placeClass(rank: number): string {
    switch (rank) {
      case 1:
        return 'border-warning/30 bg-warning/12 text-warning';
      case 2:
        return 'border-border bg-surface-2 text-text';
      default:
        return 'border-accent/25 bg-accent/12 text-accent';
    }
  }

  private async load(period: LeaderboardPeriod, type: LeaderboardType): Promise<void> {
    this.loading.set(true);
    try {
      const board = await firstValueFrom(this.api.leaderboard(period, type, 50));
      // Ignore an answer that arrives after the user already switched tabs again
      if (period === this.period() && type === this.type()) {
        this.board.set(board);
      }
    } catch {
      // Shown by the error interceptor; the previous board stays visible
    } finally {
      this.loading.set(false);
    }
  }
}

function pick(value: string | null, items: TabItem[], fallback: string): string {
  const match = items.find((i) => i.value.toLowerCase() === value?.toLowerCase());
  return match?.value ?? fallback;
}
