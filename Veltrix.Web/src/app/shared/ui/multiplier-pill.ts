import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';

/** A crash point in the "last 20" strip: red under 2x, neutral 2x-10x, accent from 10x. */
@Component({
  selector: 'vx-multiplier-pill',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'classes()' },
  template: `{{ text() }}`
})
export class MultiplierPill {
  readonly value = input<number | null>(null);
  readonly cancelled = input(false, { transform: booleanAttribute });

  protected readonly text = computed(() =>
    this.cancelled() || this.value() === null ? '—' : `${this.value()!.toFixed(2)}x`
  );

  protected readonly classes = computed(() => {
    const base =
      'num inline-flex h-7 shrink-0 items-center rounded-full border px-2.5 text-xs font-semibold ' +
      'transition-colors duration-200 ';
    const value = this.value();
    if (this.cancelled() || value === null) return base + 'border-border bg-surface-2 text-muted';
    if (value < 2) return base + 'border-danger/20 bg-danger/10 text-danger';
    if (value < 10) return base + 'border-border bg-surface-2 text-text';
    return base + 'border-accent/30 bg-accent/10 text-accent';
  });
}
