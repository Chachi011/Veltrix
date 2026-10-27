import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ProgressTone = 'primary' | 'accent' | 'success' | 'warning' | 'danger';

const FILLS: Record<ProgressTone, string> = {
  primary: 'bg-primary',
  accent: 'bg-accent',
  success: 'bg-success',
  warning: 'bg-warning',
  danger: 'bg-danger'
};

/** Thin horizontal progress, e.g. "daily limit used": <vx-progress-bar [value]="0.4" tone="warning" label="Daily limit used" /> */
@Component({
  selector: 'vx-progress-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      class="h-2 w-full overflow-hidden rounded-full bg-surface-2"
      role="progressbar"
      aria-valuemin="0"
      aria-valuemax="100"
      [attr.aria-valuenow]="percent()"
      [attr.aria-label]="label()"
    >
      <div [class]="fillClass()" [style.width.%]="percent()"></div>
    </div>
  `
})
export class ProgressBar {
  /** 0..1 (values outside are clamped). */
  readonly value = input.required<number>();
  readonly tone = input<ProgressTone>('primary');
  readonly label = input.required<string>();

  protected readonly percent = computed(() => Math.round(Math.min(1, Math.max(0, this.value())) * 100));
  protected readonly fillClass = computed(
    () => `h-full rounded-full transition-[width] duration-500 ease-out-soft ${FILLS[this.tone()]}`
  );
}
