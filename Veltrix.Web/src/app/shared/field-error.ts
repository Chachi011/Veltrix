import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

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
  return 'Invalid value.';
}

/**
 * Shows the first error of a form control once the user has touched it.
 * Default change detection on purpose: reactive-form status is not a signal, so this
 * component must be re-checked whenever its parent form is.
 */
@Component({
  selector: 'vx-field-error',
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    @if (message(); as text) {
      <span class="field-error" role="alert">{{ text }}</span>
    }
  `
})
export class FieldError {
  readonly control = input.required<AbstractControl | null>();
  /** Custom text for the "pattern" error (e.g. allowed characters). */
  readonly patternMessage = input('Invalid format.');

  message(): string | null {
    return controlErrorMessage(this.control(), this.patternMessage());
  }
}
