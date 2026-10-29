import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';

export type BadgeTone = 'neutral' | 'primary' | 'accent' | 'success' | 'danger' | 'warning';

const TONES: Record<BadgeTone, string> = {
  neutral: 'bg-surface-2 text-muted border-border',
  primary: 'bg-primary/12 text-primary-text border-primary/20',
  accent: 'bg-accent/12 text-accent border-accent/25',
  success: 'bg-success/12 text-success border-success/20',
  danger: 'bg-danger/12 text-danger border-danger/20',
  warning: 'bg-warning/12 text-warning border-warning/25'
};

const DOTS: Record<BadgeTone, string> = {
  neutral: 'bg-muted',
  primary: 'bg-primary-text',
  accent: 'bg-accent',
  success: 'bg-success',
  danger: 'bg-danger',
  warning: 'bg-warning'
};

/** Small status label: <vx-badge tone="success" dot>Cashed out</vx-badge> */
@Component({
  selector: 'vx-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'classes()' },
  template: `
    @if (dot()) {
      <span [class]="dotClass()" aria-hidden="true"></span>
    }
    <ng-content />
  `
})
export class Badge {
  readonly tone = input<BadgeTone>('neutral');
  readonly dot = input(false, { transform: booleanAttribute });

  protected readonly classes = computed(
    () =>
      'inline-flex h-6 items-center gap-1.5 rounded-full border px-2.5 text-xs font-medium whitespace-nowrap ' +
      TONES[this.tone()]
  );
  protected readonly dotClass = computed(() => `size-1.5 rounded-full ${DOTS[this.tone()]}`);
}
