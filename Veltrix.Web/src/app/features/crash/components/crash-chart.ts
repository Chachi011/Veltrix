import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  effect,
  inject,
  input,
  signal,
  viewChild
} from '@angular/core';
import { ThemeService } from '../../../core/ui/theme.service';
import { curveAt } from '../crash-math';

interface Size {
  width: number;
  height: number;
  dpr: number;
}

const PAD = { left: 48, right: 18, top: 18, bottom: 28 };

/** 1, 2, 5, 10, 20, 50... so the axis gets about 4-6 readable grid lines. */
function niceStep(raw: number): number {
  const power = 10 ** Math.floor(Math.log10(raw));
  for (const f of [1, 2, 5, 10]) {
    if (f * power >= raw) {
      return f * power;
    }
  }
  return 10 * power;
}

/**
 * Draws the multiplier curve on an HTML Canvas. Re-draws whenever an input, the size or the theme changes
 * (an effect tracks them). Canvas = one bitmap, so drawing 60 times per second stays cheap.
 */
@Component({
  selector: 'vx-crash-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<canvas #canvas aria-hidden="true"></canvas>`,
  styles: `
    :host { display: block; position: relative; width: 100%; height: 100%; }
    canvas { position: absolute; inset: 0; width: 100%; height: 100%; }
  `
})
export class CrashChart {
  /** Current multiplier (already capped at the crash point after a crash). */
  readonly multiplier = input.required<number>();
  /** Milliseconds since the round started. */
  readonly elapsedMs = input.required<number>();
  /** Draw the curve at all (false while waiting / betting). */
  readonly flying = input(false);
  readonly crashed = input(false);

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly theme = inject(ThemeService);
  private readonly size = signal<Size>({ width: 0, height: 0, dpr: 1 });
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    // Browser-only API, so it is set up after the first render
    afterNextRender(() => {
      const observer = new ResizeObserver(([entry]) =>
        this.size.set({
          width: entry.contentRect.width,
          height: entry.contentRect.height,
          dpr: window.devicePixelRatio || 1
        })
      );
      observer.observe(this.host.nativeElement);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });

    effect(() => {
      this.theme.theme(); // re-read the CSS colors when the theme changes
      this.draw(this.size(), this.multiplier(), this.elapsedMs(), this.flying(), this.crashed());
    });
  }

  private draw(size: Size, multiplier: number, elapsedMs: number, flying: boolean, crashed: boolean): void {
    const canvas = this.canvas()?.nativeElement;
    const ctx = canvas?.getContext('2d');
    if (!canvas || !ctx || size.width === 0 || size.height === 0) {
      return;
    }

    // Sharp lines on high-DPI screens: draw on a bigger bitmap, scaled down by CSS
    const pixelWidth = Math.round(size.width * size.dpr);
    const pixelHeight = Math.round(size.height * size.dpr);
    if (canvas.width !== pixelWidth || canvas.height !== pixelHeight) {
      canvas.width = pixelWidth;
      canvas.height = pixelHeight;
    }
    ctx.setTransform(size.dpr, 0, 0, size.dpr, 0, 0);
    ctx.clearRect(0, 0, size.width, size.height);

    const css = getComputedStyle(this.host.nativeElement);
    const color = (name: string) => css.getPropertyValue(name).trim();
    const lineColor = crashed ? color('--danger') : color('--curve');

    const plotW = size.width - PAD.left - PAD.right;
    const plotH = size.height - PAD.top - PAD.bottom;

    // The visible range grows with the curve, so its head stays at about 85% of the chart
    const elapsed = flying ? Math.max(0, elapsedMs) : 0;
    const tMax = Math.max(10_000, elapsed / 0.85);
    const mMax = Math.max(2, 1 + (multiplier - 1) / 0.85);

    const x = (t: number) => PAD.left + (t / tMax) * plotW;
    const y = (m: number) => PAD.top + plotH - ((m - 1) / (mMax - 1)) * plotH;

    this.drawGrid(ctx, x, y, tMax, mMax, plotW, color('--grid'), color('--text-muted'));

    if (!flying || elapsed <= 0) {
      return;
    }

    // Curve: 120 samples of the same exponential the server uses
    const samples = 120;
    ctx.beginPath();
    ctx.moveTo(x(0), y(1));
    for (let i = 1; i <= samples; i++) {
      const t = (elapsed * i) / samples;
      ctx.lineTo(x(t), y(Math.min(curveAt(t), mMax)));
    }
    const headX = x(elapsed);
    const headY = y(Math.min(multiplier, mMax));

    // Area under the curve
    const fill = ctx.createLinearGradient(0, PAD.top, 0, PAD.top + plotH);
    fill.addColorStop(0, crashed ? color('--danger-soft') : color('--curve-fill'));
    fill.addColorStop(1, 'transparent');
    ctx.save();
    ctx.lineTo(headX, y(1));
    ctx.closePath();
    ctx.fillStyle = fill;
    ctx.fill();
    ctx.restore();

    // The line itself (redrawn without the closing edges)
    ctx.beginPath();
    ctx.moveTo(x(0), y(1));
    for (let i = 1; i <= samples; i++) {
      const t = (elapsed * i) / samples;
      ctx.lineTo(x(t), y(Math.min(curveAt(t), mMax)));
    }
    ctx.strokeStyle = lineColor;
    ctx.lineWidth = 3;
    ctx.lineJoin = 'round';
    ctx.lineCap = 'round';
    ctx.stroke();

    // Head of the curve
    ctx.beginPath();
    ctx.arc(headX, headY, 6, 0, Math.PI * 2);
    ctx.fillStyle = lineColor;
    ctx.shadowColor = lineColor;
    ctx.shadowBlur = 16;
    ctx.fill();
    ctx.shadowBlur = 0;
  }

  private drawGrid(
    ctx: CanvasRenderingContext2D,
    x: (t: number) => number,
    y: (m: number) => number,
    tMax: number,
    mMax: number,
    plotW: number,
    gridColor: string,
    labelColor: string
  ): void {
    ctx.strokeStyle = gridColor;
    ctx.fillStyle = labelColor;
    ctx.lineWidth = 1;
    ctx.font = '12px system-ui, sans-serif';

    // Horizontal lines: multipliers
    const mStep = niceStep((mMax - 1) / 4);
    ctx.textAlign = 'right';
    ctx.textBaseline = 'middle';
    for (let i = 0; 1 + i * mStep <= mMax + 1e-9; i++) {
      const m = 1 + i * mStep;
      const py = Math.round(y(m)) + 0.5;
      ctx.beginPath();
      ctx.moveTo(PAD.left, py);
      ctx.lineTo(PAD.left + plotW, py);
      ctx.stroke();
      ctx.fillText(`${m < 10 ? m.toFixed(1) : m.toFixed(0)}x`, PAD.left - 8, py);
    }

    // Time labels on the bottom axis
    const tStep = niceStep(tMax / 1000 / 5) * 1000;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';
    for (let t = 0; t <= tMax + 1e-9; t += tStep) {
      ctx.fillText(`${Math.round(t / 1000)}s`, x(t), y(1) + 8);
    }
  }
}
