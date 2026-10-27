import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { AdminOverviewDto, DailyStatisticDto } from '../../core/models/api.models';
import { Card, ChartSeries, ChartView, Icons, PageHeader, Skeleton, StatCard, TabItem, Tabs } from '../../shared/ui';
import { formatDay, formatWhole } from '../../shared/format';
import { AdminNav } from './admin-nav';

const RANGES: TabItem[] = [
  { value: '7', label: '7 days' },
  { value: '30', label: '30 days' },
  { value: '90', label: '90 days' }
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
  imports: [LucideAngularModule, AdminNav, Card, ChartView, PageHeader, Skeleton, StatCard, Tabs],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-dashboard-page.html'
})
export class AdminDashboardPage {
  private readonly api = inject(AdminApi);

  protected readonly icons = Icons;
  protected readonly ranges = RANGES;
  protected readonly range = signal('30');
  protected readonly overview = signal<AdminOverviewDto | null>(null);
  protected readonly daily = signal<DailyStatisticDto[] | null>(null);

  protected readonly whole = whole;
  protected readonly vlx = vlx;
  protected readonly percent = percent;

  protected readonly dayLabels = computed(() => (this.daily() ?? []).map((d) => formatDay(d.date)));

  protected readonly activitySeries = computed<ChartSeries[]>(() => {
    const days = this.daily() ?? [];
    return [
      { label: 'Active players', data: days.map((d) => d.activeUsers), tone: 'primary', kind: 'line', fill: true },
      { label: 'New users', data: days.map((d) => d.newUsers), tone: 'success', kind: 'line' },
      { label: 'Rounds', data: days.map((d) => d.roundsPlayed), tone: 'accent', kind: 'bar', axis: 'right' }
    ];
  });

  protected readonly volumeSeries = computed<ChartSeries[]>(() => {
    const days = this.daily() ?? [];
    return [
      { label: 'Staked', data: days.map((d) => d.betsVolume), tone: 'primary' },
      { label: 'Paid out', data: days.map((d) => d.payoutVolume), tone: 'success' }
    ];
  });

  protected readonly bucketLabels = computed(() => (this.overview()?.crashPointDistribution ?? []).map((b) => b.label));
  protected readonly distributionSeries = computed<ChartSeries[]>(() => {
    const buckets = this.overview()?.crashPointDistribution ?? [];
    return [
      { label: 'Observed', data: buckets.map((b) => b.percent), tone: 'accent' },
      { label: 'Expected by the formula', data: buckets.map((b) => b.expectedPercent ?? 0), tone: 'muted' }
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
