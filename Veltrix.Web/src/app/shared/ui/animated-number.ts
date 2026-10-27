import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked
} from '@angular/core';
import { reducedMotion } from '../motion';


/** Fast at first, gently slowing down at the end. */
const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);

/**
 * A number that counts to its new value (e.g. the balance after a win) and briefly
 * tints green (up) or red (down). With reduced motion (OS setting or Settings → animations off) it changes instantly.
 */
@Component({
  selector: 'vx-animated-number',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'num transition-colors duration-500',
    '[class.text-success]': 'flash() === "up"',
    '[class.text-danger]': 'flash() === "down"'
  },
  template: `{{ text() }}`
})
export class AnimatedNumber {
  readonly value = input.required<number | null>();
  readonly decimals = input(2);
  readonly durationMs = input(650);
  readonly prefix = input('');
  readonly suffix = input('');
  /** Tint the number green/red for a moment when it changes. */
  readonly highlight = input(true, { transform: booleanAttribute });

  private readonly current = signal<number | null>(null);
  protected readonly flash = signal<'up' | 'down' | null>(null);

  private frame = 0;
  private flashTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly text = computed(() => {
    const value = this.current();
    if (value === null) {
      return '—';
    }
    const formatted = value.toLocaleString('en-US', {
      minimumFractionDigits: this.decimals(),
      maximumFractionDigits: this.decimals()
    });
    return `${this.prefix()}${formatted}${this.suffix()}`;
  });

  constructor() {
    effect(() => {
      const target = this.value();
      untracked(() => this.animateTo(target));
    });

    inject(DestroyRef).onDestroy(() => {
      cancelAnimationFrame(this.frame);
      clearTimeout(this.flashTimer);
    });
  }

  private animateTo(target: number | null): void {
    cancelAnimationFrame(this.frame);
    const from = this.current();

    // First value, "no value", or the user prefers no motion: just show it
    if (target === null || from === null || from === target || reducedMotion()) {
      this.current.set(target);
      return;
    }

    if (this.highlight()) {
      this.flash.set(target > from ? 'up' : 'down');
      clearTimeout(this.flashTimer);
      this.flashTimer = setTimeout(() => this.flash.set(null), 900);
    }

    const start = performance.now();
    const duration = this.durationMs();
    const step = (now: number) => {
      const progress = Math.min(1, (now - start) / duration);
      this.current.set(from + (target - from) * easeOutCubic(progress));
      if (progress < 1) {
        this.frame = requestAnimationFrame(step);
      }
    };
    this.frame = requestAnimationFrame(step);
  }
}
