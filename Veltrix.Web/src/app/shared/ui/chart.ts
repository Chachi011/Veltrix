import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  booleanAttribute,
  effect,
  inject,
  input,
  untracked,
  viewChild
} from '@angular/core';
// Types only: they disappear from the compiled JavaScript
import type { Chart, ChartConfiguration, ChartDataset } from 'chart.js';
import { ThemeService } from '../../core/ui/theme.service';
import { withAlpha } from '../color';

let chartClass: Promise<typeof Chart> | null = null;

/**
 * Chart.js is loaded on first use with a dynamic import(), so it becomes its own lazy chunk:
 * a page without charts never downloads it, even though this component is in the shared/ui barrel
 * (the barrel is part of the initial bundle). Only the parts the app uses are registered.
 */
function loadChartJs(): Promise<typeof Chart> {
  chartClass ??= import('chart.js').then((lib) => {
    lib.Chart.register(
      lib.BarController,
      lib.BarElement,
      lib.LineController,
      lib.LineElement,
      lib.PointElement,
      lib.DoughnutController,
      lib.ArcElement,
      lib.CategoryScale,
      lib.LinearScale,
      lib.Tooltip,
      lib.Legend,
      lib.Filler
    );
    return lib.Chart;
  });
  return chartClass;
}

export type ChartTone = 'primary' | 'accent' | 'success' | 'danger' | 'warning' | 'muted';
export type ChartKind = 'bar' | 'line' | 'doughnut';

export interface ChartSeries {
  label: string;
  data: number[];
  tone: ChartTone;
  /** Mixed charts: draw this series as a line over the bars. */
  kind?: 'bar' | 'line';
  /** Use the second value axis on the right (e.g. a count next to VLX amounts). */
  axis?: 'left' | 'right';
  /** Lines: soft fill below the line. */
  fill?: boolean;
  /** One tone per value (bars: profit green/red; doughnut: one per slice). */
  tones?: ChartTone[];
}

const TONE_VAR: Record<ChartTone, string> = {
  primary: '--primary',
  accent: '--accent',
  success: '--success',
  danger: '--danger',
  warning: '--warning',
  muted: '--text-muted'
};

const SANS = "'Inter Variable', ui-sans-serif, system-ui, sans-serif";
const MONO = "'JetBrains Mono Variable', ui-monospace, monospace";

const defaultFormat = (value: number) => new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(value);

/**
 * Chart.js wrapped in the design system: token colors read from CSS (so it follows the theme, also inside a
 * themed subtree), Inter labels, JetBrains Mono numbers, a soft grid, the card-style tooltip, no animation
 * under prefers-reduced-motion. Usage:
 *   <vx-chart type="bar" [labels]="days" [series]="[{ label: 'Bets', data: counts, tone: 'primary' }]" ariaLabel="Bets per day" />
 */
@Component({
  selector: 'vx-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative block w-full', '[style.height.px]': 'height()' },
  template: `<canvas #canvas role="img" [attr.aria-label]="ariaLabel()"></canvas>`
})
export class ChartView {
  readonly type = input<ChartKind>('bar');
  readonly labels = input.required<string[]>();
  readonly series = input.required<ChartSeries[]>();
  /** Text alternative for screen readers: what the chart shows. */
  readonly ariaLabel = input.required<string>();
  readonly height = input(260);
  /** Formats the left axis and tooltip values (e.g. "1,250 VLX", "35%"). */
  readonly format = input<(value: number) => string>(defaultFormat);
  /** Formats the right axis (series with axis: 'right'). */
  readonly formatRight = input<(value: number) => string>(defaultFormat);
  readonly legend = input(true, { transform: booleanAttribute });
  readonly stacked = input(false, { transform: booleanAttribute });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly theme = inject(ThemeService);
  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  /** The Chart.js class once its chunk has loaded; until then nothing is drawn. */
  private chartJs: typeof Chart | null = null;
  private destroyed = false;

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      void loadChartJs().then((chartJs) => {
        if (!this.destroyed) {
          this.chartJs = chartJs;
          this.render();
        }
      });
    });

    // Any input or the theme changes: update the chart in place (animated), or recreate it for a new type
    effect(() => {
      this.type();
      this.labels();
      this.series();
      this.format();
      this.formatRight();
      this.legend();
      this.stacked();
      this.theme.theme();
      untracked(() => this.chartJs && this.render());
    });

    destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.chart?.destroy();
    });
  }

  private render(): void {
    const config = this.buildConfig();

    if (this.chart && (this.chart.config as { type?: string }).type === config.type) {
      this.chart.data = config.data;
      this.chart.options = config.options ?? {};
      this.chart.update();
      return;
    }

    this.chart?.destroy();
    this.chart = new this.chartJs!(this.canvas().nativeElement, config);
  }

  private buildConfig(): ChartConfiguration {
    // Read the tokens from the host: it inherits [data-theme] from <html> or from a themed subtree
    const css = getComputedStyle(this.host.nativeElement);
    const color = (tone: ChartTone) => css.getPropertyValue(TONE_VAR[tone]).trim();
    const muted = css.getPropertyValue('--text-muted').trim();
    const text = css.getPropertyValue('--text').trim();
    const surface = css.getPropertyValue('--surface').trim();
    const border = css.getPropertyValue('--border').trim();
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const kind = this.type();
    const format = this.format();
    const formatRight = this.formatRight();
    const hasRightAxis = this.series().some((s) => s.axis === 'right');

    const datasets = this.series().map((s): ChartDataset => {
      if (kind === 'doughnut') {
        const tones = s.tones ?? s.data.map(() => s.tone);
        return {
          type: 'doughnut',
          label: s.label,
          data: s.data,
          backgroundColor: tones.map(color),
          borderColor: surface,
          borderWidth: 2,
          hoverOffset: 4
        };
      }

      const yAxisID = s.axis === 'right' ? 'y2' : 'y';
      if ((s.kind ?? kind) === 'line') {
        const c = color(s.tone);
        return {
          type: 'line',
          label: s.label,
          data: s.data,
          yAxisID,
          borderColor: c,
          backgroundColor: withAlpha(c, 0.12),
          fill: s.fill ? 'origin' : false,
          borderWidth: 2,
          // Monotone: smooth, but never above or below the real values (a plain curve would invent peaks)
          cubicInterpolationMode: 'monotone',
          pointRadius: 0,
          pointHoverRadius: 4,
          pointBackgroundColor: c,
          order: 0
        };
      }

      const fills = (s.tones ?? s.data.map(() => s.tone)).map(color);
      return {
        type: 'bar',
        label: s.label,
        data: s.data,
        yAxisID,
        backgroundColor: fills.map((c) => withAlpha(c, 0.8)),
        hoverBackgroundColor: fills,
        borderRadius: 6,
        maxBarThickness: 28,
        order: 1
      };
    });

    const tooltip = {
      backgroundColor: surface,
      borderColor: border,
      borderWidth: 1,
      titleColor: text,
      bodyColor: text,
      padding: 10,
      cornerRadius: 10,
      boxPadding: 4,
      usePointStyle: true,
      titleFont: { family: SANS, size: 12, weight: 600 },
      bodyFont: { family: MONO, size: 12 },
      callbacks: {
        label: (item: { dataset: { label?: string; yAxisID?: string }; parsed: number | { y: number } }) => {
          const value = typeof item.parsed === 'number' ? item.parsed : item.parsed.y;
          const f = item.dataset.yAxisID === 'y2' ? formatRight : format;
          return ` ${item.dataset.label ?? ''}: ${f(value)}`;
        }
      }
    };

    const legend = {
      display: this.legend(),
      position: 'bottom' as const,
      align: 'start' as const,
      labels: { color: muted, usePointStyle: true, pointStyle: 'circle' as const, boxWidth: 8, boxHeight: 8, padding: 16, font: { family: SANS, size: 12 } }
    };

    const valueTicks = (f: (v: number) => string) => ({
      color: muted,
      font: { family: MONO, size: 11 },
      maxTicksLimit: 6,
      callback: (value: string | number) => f(Number(value))
    });

    const options =
      kind === 'doughnut'
        ? { cutout: '70%', plugins: { legend, tooltip } }
        : {
            interaction: { mode: 'index' as const, intersect: false },
            plugins: { legend, tooltip },
            scales: {
              x: {
                stacked: this.stacked(),
                grid: { display: false },
                border: { color: border },
                ticks: { color: muted, font: { family: SANS, size: 11 }, maxRotation: 0, autoSkip: true, autoSkipPadding: 12 }
              },
              y: {
                stacked: this.stacked(),
                beginAtZero: true,
                grid: { color: withAlpha(muted, 0.14) },
                border: { display: false },
                ticks: valueTicks(format)
              },
              ...(hasRightAxis
                ? {
                    y2: {
                      position: 'right' as const,
                      beginAtZero: true,
                      grid: { drawOnChartArea: false },
                      border: { display: false },
                      ticks: valueTicks(formatRight)
                    }
                  }
                : {})
            }
          };

    return {
      type: kind,
      data: { labels: this.labels(), datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: reducedMotion ? false : { duration: 500 },
        ...options
      }
    } as ChartConfiguration;
  }
}
