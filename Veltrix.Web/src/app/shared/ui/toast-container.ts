import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { ToastKind, ToastStore } from '../../core/ui/toast.store';
import { Button } from './button';
import { Icons } from './icons';

/** Toasts: top of the screen on mobile (above the bottom nav and bet sheet), bottom-right on desktop. */
@Component({
  selector: 'vx-toast-container',
  imports: [LucideAngularModule, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="pointer-events-none fixed inset-x-4 top-4 z-[1000] flex flex-col gap-2 md:inset-x-auto md:top-auto md:right-6 md:bottom-6 md:w-[380px]"
    >
      @for (toast of store.toasts(); track toast.id) {
        <div
          class="pointer-events-auto flex animate-slide-up items-start gap-3 rounded-card border border-border bg-surface py-3 pr-2 pl-3.5 shadow-pop"
          [attr.role]="toast.kind === 'error' ? 'alert' : 'status'"
        >
          <span class="mt-0.5 grid size-6 shrink-0 place-items-center rounded-full" [class]="iconWrap(toast.kind)">
            <lucide-icon [img]="icon(toast.kind)" [size]="14" aria-hidden="true" />
          </span>
          <p class="min-w-0 flex-1 pt-0.5 text-[13px] leading-5">{{ toast.message }}</p>
          <button vxButton type="button" variant="ghost" size="icon-sm" [attr.aria-label]="'Dismiss' | t" (click)="store.dismiss(toast.id)">
            <lucide-icon [img]="icons.close" [size]="14" aria-hidden="true" />
          </button>
        </div>
      }
    </div>
  `
})
export class ToastContainer {
  protected readonly store = inject(ToastStore);
  protected readonly icons = Icons;

  protected icon(kind: ToastKind) {
    return { success: Icons.success, error: Icons.alert, info: Icons.info }[kind];
  }

  protected iconWrap(kind: ToastKind): string {
    const tone = {
      success: 'bg-success/15 text-success',
      error: 'bg-danger/15 text-danger',
      info: 'bg-primary/15 text-primary-text'
    }[kind];
    return `mt-0.5 grid size-6 shrink-0 place-items-center rounded-full ${tone}`;
  }
}
