import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Icons } from './icons';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'success' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg' | 'xl' | 'icon' | 'icon-sm';

const BASE =
  'relative inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-control font-semibold select-none ' +
  'transition-[background-color,border-color,color,box-shadow,transform,filter] duration-200 ease-out-soft ' +
  'active:translate-y-px disabled:pointer-events-none disabled:opacity-50 ' +
  'aria-disabled:pointer-events-none aria-disabled:opacity-50';

// Full class strings (no string building), so Tailwind can find them in this file
const VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-primary text-on-primary shadow-soft hover:bg-primary-hover',
  secondary: 'border border-border bg-surface-2 text-text hover:border-muted/40',
  outline: 'border border-border bg-transparent text-text hover:bg-surface-2',
  ghost: 'bg-transparent text-muted hover:bg-surface-2 hover:text-text',
  success: 'bg-success text-on-success shadow-soft hover:brightness-110',
  // Soft style: red text on a red tint passes AA in both themes, white on #EF4444 would not
  danger: 'bg-danger/12 text-danger hover:bg-danger/20'
};

const SIZES: Record<ButtonSize, string> = {
  sm: 'h-8 px-3 text-[13px]',
  md: 'h-10 px-4 text-sm',
  lg: 'h-12 px-5 text-[15px]',
  xl: 'h-14 px-6 text-base',
  icon: 'size-10',
  'icon-sm': 'size-8'
};

const ICON_SIZE: Record<ButtonSize, number> = { sm: 14, md: 16, lg: 18, xl: 20, icon: 18, 'icon-sm': 16 };

/**
 * Styles a NATIVE <button> or <a>, so keyboard, form submit and screen-reader behavior stay built-in.
 * <button vxButton variant="primary" [loading]="saving()">Save</button>
 */
@Component({
  selector: 'button[vxButton], a[vxButton]',
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.disabled]': 'isDisabled() ? "" : null',
    '[attr.aria-disabled]': 'isDisabled() || null',
    '[attr.aria-busy]': 'loading() || null'
  },
  template: `
    @if (loading()) {
      <lucide-icon [img]="icons.loader" [size]="iconSize()" class="animate-spin" aria-hidden="true" />
    }
    <ng-content />
  `
})
export class Button {
  readonly variant = input<ButtonVariant>('secondary');
  readonly size = input<ButtonSize>('md');
  readonly block = input(false, { transform: booleanAttribute });
  readonly loading = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });

  protected readonly icons = Icons;
  protected readonly isDisabled = computed(() => this.disabled() || this.loading());
  protected readonly iconSize = computed(() => ICON_SIZE[this.size()]);
  protected readonly classes = computed(
    () => `${BASE} ${VARIANTS[this.variant()]} ${SIZES[this.size()]} ${this.block() ? 'w-full' : ''}`
  );
}
