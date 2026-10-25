import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Circular progress (e.g. the betting countdown). Anything projected is centered inside the ring. */
@Component({
  selector: 'vx-progress-ring',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'relative inline-grid place-items-center',
    '[style.width.px]': 'size()',
    '[style.height.px]': 'size()',
    role: 'progressbar',
    '[attr.aria-valuenow]': 'percent()',
    'aria-valuemin': '0',
    'aria-valuemax': '100'
  },
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" class="absolute inset-0 -rotate-90" aria-hidden="true">
      <circle [attr.cx]="center()" [attr.cy]="center()" [attr.r]="radius()" fill="none" stroke="var(--border)" [attr.stroke-width]="stroke()" />
      <circle
        [attr.cx]="center()"
        [attr.cy]="center()"
        [attr.r]="radius()"
        fill="none"
        [attr.stroke]="color()"
        [attr.stroke-width]="stroke()"
        stroke-linecap="round"
        [attr.stroke-dasharray]="circumference()"
        [attr.stroke-dashoffset]="offset()"
        class="transition-[stroke-dashoffset] duration-200 ease-linear"
      />
    </svg>
    <div class="relative text-center"><ng-content /></div>
  `
})
export class ProgressRing {
  /** 0 … 1 */
  readonly value = input(0);
  readonly size = input(72);
  readonly stroke = input(5);
  readonly color = input('var(--primary)');

  protected readonly center = computed(() => this.size() / 2);
  protected readonly radius = computed(() => (this.size() - this.stroke()) / 2);
  protected readonly circumference = computed(() => 2 * Math.PI * this.radius());
  protected readonly offset = computed(() => this.circumference() * (1 - Math.min(1, Math.max(0, this.value()))));
  protected readonly percent = computed(() => Math.round(Math.min(1, Math.max(0, this.value())) * 100));
}
