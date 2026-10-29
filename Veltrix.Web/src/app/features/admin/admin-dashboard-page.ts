import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { I18n, mark } from '../../core/i18n/i18n';
import { TranslateLabelsPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { AdminOverviewDto, DailyStatisticDto } from '../../core/models/api.models';
import { Card, ChartSeries, ChartView, Icons, PageHeader, Skeleton, StatCard, TabItem, Tabs } from '../../shared/ui';
import { formatDay, formatWhole } from '../../shared/format';
import { AdminNav } from './admin-nav';

const RANGES: TabItem[] = [
  { value: '7', label: mark('7 days') },
  { value: '30', label: mark('30 days') },
  { value: '90', label: mark('90 days') }
];

const whole = formatWhole;
const vlx = (v: number) => `${whole(v)} VLX`;
const percent = (v: number) => `${v.toFixed(1)}%`;

/** yyyy-MM-dd of a UTC day, n days before today. */
function utcDay(daysAgo: number): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - daysAgo);
  return d.toISOString().slice(0, 10);
}

/** Admin dashboard: today's numbers and the trends of the last days (spec: active users, rounds per day, volume, crash points). */
@Component({
  selector: 'vx-admin-dashboard-page',
  imports: [LucideAngularModule, AdminNav, Card, ChartView, PageHeader, Skeleton, StatCard, Tabs, TranslatePipe, TranslateLabelsPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-dashboard-page.html'
})
export class AdminDashboardPage {
  private readonly api = inject(AdminApi);
  private readonly i18n = inject(I18n);

  protected readonly icons = Icons;
  protected readonly ranges = RANGES;
  protected readonly range = signal('30');
  protected readonly overview = signal<AdminOverviewDto | null>(null);
  protected readonly daily = signal<DailyStatisticDto[] | null>(null);

  protected readonly whole = whole;
  protected readonly vlx = vlx;
  protected readonly percent = percent;

  protected readonly dayLabels = computed(() => (this.daily() ?? []).map((d) => formatDay(d.date, this.i18n.locale())));

  protected readonly activitySeries = computed<ChartSeries[]>(() => {
    const days = this.daily() ?? [];
    return [
      { label: this.i18n.t('Active players'), data: days.map((d) => d.activeUsers), tone: 'primary', kind: 'line', fill: true },
      { label: this.i18n.t('New users'), data: days.map((d) => d.newUsers), tone: 'success', kind: 'line' },
      { label: this.i18n.t('Rounds'), data: days.map((d) => d.roundsPlayed), tone: 'accent', kind: 'bar', axis: 'right' }
    ];
  });

  protected readonly volumeSeries = computed<ChartSeries[]>(() => {
    const days = this.daily() ?? [];
    return [
      { label: this.i18n.t('Staked'), data: days.map((d) => d.betsVolume), tone: 'primary' },
      { label: this.i18n.t('Paid out'), data: days.map((d) => d.payoutVolume), tone: 'success' }
    ];
  });

  protected readonly bucketLabels = computed(() => (this.overview()?.crashPointDistribution ?? []).map((b) => b.label));
  protected readonly distributionSeries = computed<ChartSeries[]>(() => {
    const buckets = this.overview()?.crashPointDistribution ?? [];
    return [
      { label: this.i18n.t('Observed'), data: buckets.map((b) => b.percent), tone: 'accent' },
      { label: this.i18n.t('Expected by the formula'), data: buckets.map((b) => b.expectedPercent ?? 0), tone: 'muted' }
    ];
  });

  constructor() {
    void this.loadOverview();
    effect(() => {
      const days = Number(this.range());
      untracked(() => void this.loadDaily(days));
    });
  }

  private async loadOverview(): Promise<void> {
    this.overview.set(await firstValueFrom(this.api.overview()).catch(() => null));
  }

  private async loadDaily(days: number): Promise<void> {
    const rows = await firstValueFrom(this.api.daily(utcDay(days - 1), utcDay(0))).catch(() => null);
    if (rows && days === Number(this.range())) {
      this.daily.set(rows);
    }
  }
}
