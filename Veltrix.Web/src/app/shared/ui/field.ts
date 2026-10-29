import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { I18n } from '../../core/i18n/i18n';
import { Icons } from './icons';

/**
 * The first error of a control as a sentence in the current language, once the user has interacted with it.
 * Null when valid. `patternMessage` must already be translated (the caller knows what the pattern means).
 */
export function controlErrorMessage(control: AbstractControl | null, i18n: I18n, patternMessage?: string): string | null {
  if (!control || !control.errors || !(control.touched || control.dirty)) {
    return null;
  }

  const e = control.errors;
  if (e['server']) return i18n.server(e['server']); // a field error sent by the API
  if (e['required']) return i18n.t('This field is required.');
  if (e['email']) return i18n.t('Enter a valid email address.');
  if (e['minlength']) return i18n.t('At least {count} characters.', { count: e['minlength'].requiredLength });
  if (e['maxlength']) return i18n.t('At most {count} characters.', { count: e['maxlength'].requiredLength });
  if (e['min']) return i18n.t('Minimum is {value}.', { value: e['min'].min });
  if (e['max']) return i18n.t('Maximum is {value}.', { value: e['max'].max });
  if (e['pattern']) return patternMessage ?? i18n.t('Invalid format.');
  if (e['passwordStrength']) return i18n.t('Use 8+ characters with an uppercase letter, a lowercase letter and a digit.');
  if (e['adult']) return i18n.t('You must be at least 18 years old.');
  if (e['mismatch']) return i18n.t('Passwords do not match.');
  if (e['decimals']) return i18n.t('At most 2 decimal places.');
  if (e['maxBelowMin']) return i18n.t('Must be at least the minimum.');
  return i18n.t('Invalid value.');
}

/**
 * Label + input + hint/error, with consistent spacing. The input itself is projected:
 *
 * <vx-field [label]="'Email' | t" for="email" [control]="form.controls.email">
 *   <input id="email" class="vx-input" formControlName="email" />
 * </vx-field>
 *
 * Label, hint and patternMessage arrive already translated. Default change detection: reactive-form validity
 * is not a signal, so the error must be re-checked with the form.
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
  readonly patternMessage = input<string>();

  private readonly i18n = inject(I18n);
  protected readonly icons = Icons;

  protected error(): string | null {
    return controlErrorMessage(this.control(), this.i18n, this.patternMessage());
  }
}
