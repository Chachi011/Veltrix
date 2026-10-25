import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Placeholder with the shape of the content that is loading (instead of a spinner):
 * the layout doesn't jump when the data arrives. The shimmer stops under prefers-reduced-motion.
 */
@Component({
  selector: 'vx-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[style.width]': 'width()',
    '[style.height]': 'height()',
    'aria-hidden': 'true'
  },
  template: ''
})
export class Skeleton {
  readonly width = input('100%');
  readonly height = input('1rem');
  readonly shape = input<'line' | 'rect' | 'circle'>('line');

  protected readonly classes = computed(() => {
    const radius = { line: 'rounded-md', rect: 'rounded-card', circle: 'rounded-full' }[this.shape()];
    return `block vx-skeleton animate-shimmer ${radius}`;
  });
}
