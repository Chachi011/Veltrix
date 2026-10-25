import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Button } from './button';
import { Icons } from './icons';

/** Copies text to the clipboard and shows a check mark for a moment. */
@Component({
  selector: 'vx-copy-button',
  imports: [LucideAngularModule, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      vxButton
      type="button"
      variant="ghost"
      size="icon-sm"
      class="size-7!"
      [attr.aria-label]="copied() ? 'Copied' : label()"
      [title]="copied() ? 'Copied!' : label()"
      (click)="copy()"
    >
      <lucide-icon [img]="copied() ? icons.check : icons.copy" [size]="14" [class.text-success]="copied()" aria-hidden="true" />
    </button>
    <span class="sr-only" aria-live="polite">{{ copied() ? 'Copied to clipboard' : '' }}</span>
  `
})
export class CopyButton {
  readonly text = input.required<string>();
  readonly label = input('Copy');

  protected readonly icons = Icons;
  protected readonly copied = signal(false);
  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  protected async copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.text());
      this.copied.set(true);
      clearTimeout(this.timer);
      this.timer = setTimeout(() => this.copied.set(false), 1500);
    } catch {
      // Clipboard blocked (e.g. insecure context): the full value is still in the title attribute
    }
  }
}
