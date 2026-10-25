import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Renderer2,
  afterNextRender,
  inject,
  signal
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Icons } from './icons';

/**
 * Adds a show/hide button to a password input. The input stays a normal projected <input>,
 * so formControlName, autocomplete and validation work exactly as before:
 *
 * <vx-password-input><input id="password" class="vx-input" type="password" formControlName="password" /></vx-password-input>
 */
@Component({
  selector: 'vx-password-input',
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'relative block' },
  template: `
    <ng-content />
    <button
      type="button"
      class="absolute top-1/2 right-1 grid size-8 -translate-y-1/2 place-items-center rounded-lg text-muted transition-colors duration-200 hover:bg-surface hover:text-text"
      [attr.aria-label]="visible() ? 'Hide password' : 'Show password'"
      [attr.aria-pressed]="visible()"
      (click)="toggle()"
    >
      <lucide-icon [img]="visible() ? icons.eyeOff : icons.eye" [size]="16" aria-hidden="true" />
    </button>
  `
})
export class PasswordInput {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);
  protected readonly icons = Icons;
  protected readonly visible = signal(false);

  constructor() {
    // Room on the right for the toggle button
    afterNextRender(() => {
      const field = this.input();
      if (field) {
        this.renderer.setStyle(field, 'padding-right', '2.75rem');
      }
    });
  }

  protected toggle(): void {
    const field = this.input();
    if (!field) {
      return;
    }
    this.visible.update((v) => !v);
    this.renderer.setAttribute(field, 'type', this.visible() ? 'text' : 'password');
  }

  private input(): HTMLInputElement | null {
    return this.host.nativeElement.querySelector('input');
  }
}
