import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { controlErrorMessage } from '../field-error';
import { Icons } from './icons';

/**
 * Label + input + hint/error, with consistent spacing. The input itself is projected:
 *
 * <vx-field label="Email" for="email" [control]="form.controls.email">
 *   <input id="email" class="vx-input" formControlName="email" />
 * </vx-field>
 *
 * Default change detection: reactive-form validity is not a signal, so the error must be re-checked with the form.
 */
@Component({
  selector: 'vx-field',
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.Default,
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    @if (label()) {
      <label class="vx-label" [attr.for]="for()">{{ label() }}</label>
    }
    <ng-content />
    @if (error(); as message) {
      <p class="flex items-center gap-1.5 text-xs text-danger" role="alert">
        <lucide-icon [img]="icons.alert" [size]="13" aria-hidden="true" />{{ message }}
      </p>
    } @else if (hint()) {
      <p class="text-xs text-muted">{{ hint() }}</p>
    }
  `
})
export class Field {
  readonly label = input<string>();
  /** id of the input, so clicking the label focuses it. */
  readonly for = input<string>();
  readonly hint = input<string>();
  readonly control = input<AbstractControl | null>(null);
  readonly patternMessage = input('Invalid format.');

  protected readonly icons = Icons;

  protected error(): string | null {
    return controlErrorMessage(this.control(), this.patternMessage());
  }
}
