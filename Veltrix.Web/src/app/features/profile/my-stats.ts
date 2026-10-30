import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { StatsApi } from '../../core/api/stats.api';
import { I18n, mark } from '../../core/i18n/i18n';
import { TranslateLabelsPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { MyStatsDto } from '../../core/models/api.models';
import { LimitsStore } from '../../core/state/limits.store';
import { formatDay, formatWhole } from '../../shared/format';
import { Button, Card, ChartSeries, ChartView, EmptyState, Icons, Skeleton, StatCard, TabItem, Tabs } from '../../shared/ui';

const RANGES: TabItem[] = [
  { value: '7', label: mark('7 days') },
  { value: '30', label: mark('30 days') },
  { value: '90', label: mark('90 days') }
];

/** Personal statistics with charts (profile page): bets, win rate, average cash-out, results per day. */
@Component({
  selector: 'vx-my-stats',
  imports: [RouterLink, LucideAngularModule, Button, Card, ChartView, EmptyState, Skeleton, StatCard, Tabs, TranslatePipe, TranslateLabelsPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (stats(); as s) {
      @if (s.totalBets === 0) {
        <vx-card [title]="'Statistics' | t">
          <vx-empty-state
            [icon]="icons.chartColumn"
            [title]="'No settled bets yet' | t"
            [message]="'Your win rate, cash-outs and daily results will appear here.' | t"
          >
            @if (limits.canPlayNow()) {
              <a vxButton routerLink="/crash" variant="primary" size="sm">
                <lucide-icon [img]="icons.rocket" [size]="15" aria-hidden="true" />{{ 'Play a round' | t }}
              </a>
            }
          </vx-empty-state>
        </vx-card>
      } @else {
        <div class="grid grid-cols-2 gap-4 md:gap-6 lg:grid-cols-4">
          <vx-stat-card
            [label]="'Win rate' | t"
            [value]="s.winRate"
            [decimals]="1"
            unit="%"
            [icon]="icons.trendingUp"
            [hint]="'{count} of {total} cashed out' | t: { count: s.cashOutCount, total: s.totalBets }"
          />
          <vx-stat-card [label]="'Average cash-out' | t" [value]="s.averageCashOut" [decimals]="2" unit="x" [icon]="icons.zap" />
          <vx-stat-card [label]="'Net result' | t" [value]="s.netProfit" [decimals]="2" unit="VLX" [icon]="icons.vlx" [hint]="'Payouts minus stakes' | t" />
          <vx-stat-card
            [label]="'Total staked' | t"
            [value]="s.totalWagered"
            [decimals]="2"
            unit="VLX"
            [icon]="icons.wallet"
            [hint]="'Average bet {amount}' | t: { amount: s.averageBet.toFixed(2) }"
          />
        </div>

        <div class="mt-4 grid gap-4 md:mt-6 md:gap-6 lg:grid-cols-3">
          <vx-card
            class="lg:col-span-2"
            [title]="'Results per day' | t"
            [subtitle]="'Cashed out and lost bets, with the net result of each day (UTC days).' | t"
          >
            <vx-tabs card-actions [items]="ranges | tLabels" [(value)]="range" [label]="'Range' | t" />
            <vx-chart
              type="bar"
              [labels]="dayLabels()"
              [series]="dailySeries()"
              [format]="count"
              [formatRight]="vlx"
              stacked
              [height]="260"
              [ariaLabel]="'Bets per day, split into cashed out and lost, with the net result per day' | t"
            />
          </vx-card>

          <vx-card [title]="'Win rate' | t" [subtitle]="'Share of bets you cashed out.' | t">
            <div class="relative">
              <vx-chart
                type="doughnut"
                [labels]="winLossLabels()"
                [series]="winLossSeries()"
                [format]="count"
                [height]="220"
                [ariaLabel]="'Win rate {rate} percent: {won} cashed out, {lost} lost' | t: { rate: s.winRate, won: s.cashOutCount, lost: s.lostCount }"
              />
              <!-- The number in the middle of the ring (above the legend) -->
              <div class="pointer-events-none absolute inset-x-0 top-0 grid h-[176px] place-items-center" aria-hidden="true">
                <div class="text-center">
                  <p class="num text-2xl font-semibold">{{ s.winRate.toFixed(1) }}%</p>
                  <p class="text-xs text-muted">{{ '{count} bets' | t: { count: s.totalBets } }}</p>
                </div>
              </div>
            </div>
          </vx-card>

          <vx-card
            class="lg:col-span-3"
            [title]="'Where you cash out' | t"
            [subtitle]="'How many of your cash-outs landed in each multiplier range.' | t"
          >
            <vx-chart
              type="bar"
              [labels]="bucketLabels()"
              [series]="bucketSeries()"
              [format]="count"
              [legend]="false"
              [height]="220"
              [ariaLabel]="'Number of cash-outs per multiplier range' | t"
            />
          </vx-card>
        </div>
      }
    } @else {
      <div class="grid grid-cols-2 gap-4 md:gap-6 lg:grid-cols-4">
        @for (label of skeletonLabels; track label) {
          <vx-stat-card [label]="label | t" [value]="null" />
        }
      </div>
      <vx-card class="mt-4 md:mt-6">
        <vx-skeleton height="260px" shape="rect" />
      </vx-card>
    }
  `
})
export class MyStats {
  private readonly api = inject(StatsApi);
  private readonly i18n = inject(I18n);
  protected readonly limits = inject(LimitsStore);

  protected readonly icons = Icons;
  protected readonly ranges = RANGES;
  protected readonly skeletonLabels = [mark('Win rate'), mark('Average cash-out'), mark('Net result'), mark('Total staked')];
  protected readonly range = signal('30');
  protected readonly stats = signal<MyStatsDto | null>(null);

  protected readonly count = formatWhole;
  protected readonly vlx = (v: number) => `${formatWhole(v)} VLX`;

  protected readonly dayLabels = computed(() => (this.stats()?.daily ?? []).map((d) => formatDay(d.date, this.i18n.locale())));
  protected readonly winLossLabels = computed(() => [this.i18n.t('Cashed out'), this.i18n.t('Lost')]);

  protected readonly dailySeries = computed<ChartSeries[]>(() => {
    const daily = this.stats()?.daily ?? [];
    return [
      { label: this.i18n.t('Cashed out'), data: daily.map((d) => d.cashOuts), tone: 'success' },
      { label: this.i18n.t('Lost'), data: daily.map((d) => d.bets - d.cashOuts), tone: 'danger' },
      { label: this.i18n.t('Net result (VLX)'), data: daily.map((d) => d.profit), tone: 'accent', kind: 'line', axis: 'right' }
    ];
  });

  protected readonly winLossSeries = computed<ChartSeries[]>(() => {
    const s = this.stats();
    return [{ label: this.i18n.t('Bets'), data: [s?.cashOutCount ?? 0, s?.lostCount ?? 0], tone: 'success', tones: ['success', 'danger'] }];
  });

  protected readonly bucketLabels = computed(() => (this.stats()?.cashOutDistribution ?? []).map((b) => b.label));
  protected readonly bucketSeries = computed<ChartSeries[]>(() => [
    { label: this.i18n.t('Cash-outs'), data: (this.stats()?.cashOutDistribution ?? []).map((b) => b.count), tone: 'primary' }
  ]);

  constructor() {
    effect(() => {
      const days = Number(this.range());
      untracked(() => void this.load(days));
    });
  }

  private async load(days: number): Promise<void> {
    const stats = await firstValueFrom(this.api.myStats(days)).catch(() => null);
    if (stats && days === Number(this.range())) {
      this.stats.set(stats);
    }
  }
}
