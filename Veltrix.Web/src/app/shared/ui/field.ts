import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Icons } from './icons';

/** The first error of a control as a sentence, once the user has interacted with it. Null when valid. */
export function controlErrorMessage(control: AbstractControl | null, patternMessage = 'Invalid format.'): string | null {
  if (!control || !control.errors || !(control.touched || control.dirty)) {
    return null;
  }

  const e = control.errors;
  if (e['server']) return e['server'];
  if (e['required']) return 'This field is required.';
  if (e['email']) return 'Enter a valid email address.';
  if (e['minlength']) return `At least ${e['minlength'].requiredLength} characters.`;
  if (e['maxlength']) return `At most ${e['maxlength'].requiredLength} characters.`;
  if (e['min']) return `Minimum is ${e['min'].min}.`;
  if (e['max']) return `Maximum is ${e['max'].max}.`;
  if (e['pattern']) return patternMessage;
  if (e['passwordStrength']) return 'Use 8+ characters with an uppercase letter, a lowercase letter and a digit.';
  if (e['adult']) return 'You must be at least 18 years old.';
  if (e['mismatch']) return 'Passwords do not match.';
  if (e['decimals']) return 'At most 2 decimal places.';
  if (e['maxBelowMin']) return 'Must be at least the minimum.';
  return 'Invalid value.';
}

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
