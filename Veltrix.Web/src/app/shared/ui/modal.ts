import { DIALOG_DATA, Dialog, DialogRef } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { ComponentType } from '@angular/cdk/portal';
import { ChangeDetectionStrategy, Component, Injectable, inject, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { Button, ButtonVariant } from './button';
import { Icons } from './icons';

/**
 * Visual frame of every dialog: glass panel, title, close button, body, footer ([modal-footer]).
 * Focus trapping, Escape-to-close, backdrop and restoring focus afterwards come from the Angular CDK Dialog.
 */
@Component({
  selector: 'vx-modal',
  imports: [LucideAngularModule, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="glass animate-pop-in rounded-card border border-border shadow-pop">
      <header class="flex items-start gap-3 px-6 pt-5">
        <div class="min-w-0 flex-1">
          <h2 class="text-base font-semibold">{{ title() }}</h2>
          @if (description()) {
            <p class="mt-1 text-[13px] text-muted">{{ description() }}</p>
          }
        </div>
        <button vxButton type="button" variant="ghost" size="icon-sm" [attr.aria-label]="'Close' | t" (click)="dialogRef.close()">
          <lucide-icon [img]="icons.close" [size]="16" aria-hidden="true" />
        </button>
      </header>
      <div class="px-6 py-4 empty:hidden"><ng-content /></div>
      <footer class="flex flex-wrap justify-end gap-2 px-6 pb-5 empty:hidden"><ng-content select="[modal-footer]" /></footer>
    </div>
  `
})
export class ModalShell {
  readonly title = input.required<string>();
  readonly description = input<string>();
  protected readonly dialogRef = inject(DialogRef);
  protected readonly icons = Icons;
}

/**
 * Visual frame of a drawer (a panel sliding in from the right, e.g. the admin user details):
 * title, close button, scrolling body, optional footer ([drawer-footer]). Full width on phones.
 * Open it with ModalService.openDrawer(...).
 */
@Component({
  selector: 'vx-drawer',
  imports: [LucideAngularModule, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block h-full' },
  template: `
    <div class="flex h-full animate-drawer-in flex-col border-l border-border bg-surface shadow-pop">
      <header class="flex items-start gap-3 border-b border-border px-5 py-4 md:px-6">
        <div class="min-w-0 flex-1">
          <h2 class="truncate text-base font-semibold">{{ title() }}</h2>
          @if (description()) {
            <p class="mt-0.5 truncate text-[13px] text-muted">{{ description() }}</p>
          }
        </div>
        <button vxButton type="button" variant="ghost" size="icon-sm" [attr.aria-label]="'Close' | t" (click)="dialogRef.close()">
          <lucide-icon [img]="icons.close" [size]="16" aria-hidden="true" />
        </button>
      </header>
      <div class="min-h-0 flex-1 overflow-y-auto px-5 py-5 md:px-6"><ng-content /></div>
      <footer class="border-t border-border px-5 py-4 empty:hidden md:px-6"><ng-content select="[drawer-footer]" /></footer>
    </div>
  `
})
export class DrawerShell {
  readonly title = input.required<string>();
  readonly description = input<string>();
  protected readonly dialogRef = inject(DialogRef);
  protected readonly icons = Icons;
}

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  tone?: Extract<ButtonVariant, 'primary' | 'danger' | 'success'>;
}

/** Ready-made "Are you sure?" dialog. Use ModalService.confirm(...). */
@Component({
  selector: 'vx-confirm-dialog',
  imports: [ModalShell, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-modal [title]="data.title" [description]="data.message">
      <ng-container modal-footer>
        <button vxButton type="button" variant="ghost" (click)="ref.close(false)">{{ data.cancelText ?? ('Cancel' | t) }}</button>
        <button vxButton type="button" [variant]="data.tone ?? 'primary'" (click)="ref.close(true)">
          {{ data.confirmText ?? ('Confirm' | t) }}
        </button>
      </ng-container>
    </vx-modal>
  `
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmOptions>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
}

@Injectable({ providedIn: 'root' })
export class ModalService {
  private readonly dialog = inject(Dialog);
  private readonly overlay = inject(Overlay);

  /** Opens any component as a modal dialog. `ariaLabel` names the dialog for screen readers. */
  open<TResult, TData, TComponent>(
    component: ComponentType<TComponent>,
    options: { data?: TData; ariaLabel: string; width?: string }
  ): DialogRef<TResult, TComponent> {
    return this.dialog.open<TResult, TData, TComponent>(component, {
      data: options.data,
      ariaLabel: options.ariaLabel,
      width: options.width ?? '440px',
      maxWidth: 'calc(100vw - 32px)',
      backdropClass: 'vx-backdrop',
      panelClass: 'vx-dialog-panel',
      autoFocus: 'first-tabbable',
      restoreFocus: true
    });
  }

  /** Opens a component as a drawer on the right (same focus trap, Escape and backdrop as a modal). */
  openDrawer<TResult, TData, TComponent>(
    component: ComponentType<TComponent>,
    options: { data?: TData; ariaLabel: string; width?: string }
  ): DialogRef<TResult, TComponent> {
    return this.dialog.open<TResult, TData, TComponent>(component, {
      data: options.data,
      ariaLabel: options.ariaLabel,
      width: options.width ?? '480px',
      maxWidth: '100vw',
      height: '100%',
      positionStrategy: this.overlay.position().global().right('0').top('0'),
      backdropClass: 'vx-backdrop',
      panelClass: 'vx-drawer-panel',
      autoFocus: 'first-tabbable',
      restoreFocus: true
    });
  }

  /** Resolves true when the user confirms; false on cancel, Escape or a backdrop click. */
  async confirm(options: ConfirmOptions): Promise<boolean> {
    const ref = this.open<boolean, ConfirmOptions, ConfirmDialog>(ConfirmDialog, {
      data: options,
      ariaLabel: options.title
    });
    return (await firstValueFrom(ref.closed)) === true;
  }
}
