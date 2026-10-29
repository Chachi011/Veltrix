import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { AnimatedNumber } from './animated-number';
import { Icons } from './icons';
import { Skeleton } from './skeleton';

/** One key figure: label, big number, optional change vs. a previous period. Null value = loading skeleton. */
@Component({
  selector: 'vx-stat-card',
  imports: [LucideAngularModule, AnimatedNumber, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block min-w-0 rounded-card border border-border bg-surface p-5 shadow-card' },
  template: `
    <div class="flex items-center justify-between gap-3">
      <span class="text-[13px] font-medium text-muted">{{ label() }}</span>
      @if (icon(); as img) {
        <span class="grid size-8 place-items-center rounded-control bg-surface-2 text-muted">
          <lucide-icon [img]="img" [size]="16" aria-hidden="true" />
        </span>
      }
    </div>

    <div class="mt-3 flex flex-wrap items-baseline gap-x-1.5">
      @if (value() === null) {
        <vx-skeleton width="60%" height="2rem" />
      } @else {
        <vx-animated-number class="text-2xl leading-tight font-semibold sm:text-[28px]" [value]="value()" [decimals]="decimals()" />
        @if (unit()) {
          <span class="text-[13px] font-medium text-muted">{{ unit() }}</span>
        }
      }
    </div>

    @if (delta() !== null) {
      <div class="mt-3 flex flex-wrap items-center gap-x-1.5 gap-y-0.5 text-xs">
        <span class="inline-flex items-center gap-1 font-semibold" [class]="deltaClass()">
          <lucide-icon [img]="delta()! >= 0 ? icons.trendingUp : icons.trendingDown" [size]="14" aria-hidden="true" />
          <span class="num">{{ delta()! >= 0 ? '+' : '' }}{{ delta()!.toFixed(1) }}%</span>
        </span>
        @if (hint()) {
          <span class="text-muted">{{ hint() }}</span>
        }
      </div>
    } @else if (hint()) {
      <p class="mt-3 text-xs text-muted">{{ hint() }}</p>
    }
  `
})
export class StatCard {
  readonly label = input.required<string>();
  readonly value = input<number | null>(null);
  readonly decimals = input(0);
  readonly unit = input<string>();
  readonly icon = input<LucideIconData>();
  /** Change in percent; positive = green, negative = red. */
  readonly delta = input<number | null>(null);
  readonly hint = input<string>();

  protected readonly icons = Icons;
  protected readonly deltaClass = computed(() =>
    (this.delta() ?? 0) >= 0 ? 'inline-flex items-center gap-1 font-semibold text-success' : 'inline-flex items-center gap-1 font-semibold text-danger'
  );
}
