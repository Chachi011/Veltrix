import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

export type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

const SIZES: Record<AvatarSize, string> = {
  xs: 'size-6 text-[10px]',
  sm: 'size-8 text-xs',
  md: 'size-10 text-sm',
  lg: 'size-16 text-xl',
  xl: 'size-24 text-3xl'
};

/** Profile picture, or initials on a color derived from the name (same name = same color everywhere). */
@Component({
  selector: 'vx-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[style.background-color]': 'showImage() ? null : tint()',
    role: 'img',
    '[attr.aria-label]': 'name()'
  },
  template: `
    @if (showImage()) {
      <img [src]="src()" alt="" class="size-full object-cover" (error)="failed.set(true)" />
    } @else {
      {{ initials() }}
    }
  `
})
export class Avatar {
  readonly src = input<string | null | undefined>(null);
  readonly name = input('');
  readonly size = input<AvatarSize>('md');

  protected readonly failed = signal(false);
  protected readonly showImage = computed(() => !!this.src() && !this.failed());

  protected readonly initials = computed(() => {
    const parts = this.name().replace(/[_\-.]+/g, ' ').trim().split(/\s+/);
    return ((parts[0]?.[0] ?? '?') + (parts[1]?.[0] ?? '')).toUpperCase();
  });

  /** Hue from a simple string hash; a light tint so the text color (var(--text)) stays readable. */
  protected readonly tint = computed(() => {
    let hash = 0;
    for (const char of this.name()) {
      hash = (hash * 31 + char.charCodeAt(0)) | 0;
    }
    return `hsl(${Math.abs(hash) % 360} 70% 55% / 0.22)`;
  });

  protected readonly classes = computed(
    () =>
      'inline-grid shrink-0 place-items-center overflow-hidden rounded-full border border-border ' +
      'bg-surface-2 font-semibold text-text select-none ' +
      SIZES[this.size()]
  );
}
