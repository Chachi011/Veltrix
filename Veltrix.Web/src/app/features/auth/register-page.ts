import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { ToastStore } from '../../core/ui/toast.store';
import { FieldError } from '../../shared/field-error';
import { NICKNAME_PATTERN, fieldsMatch, minAge, strongPassword } from '../../shared/validators';

@Component({
  selector: 'vx-register-page',
  imports: [ReactiveFormsModule, RouterLink, FieldError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-narrow">
      <div class="card">
        <h1>Create account</h1>
        <p class="muted">You start with 1,000 virtual VLX points. No real money, ever.</p>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
            <vx-field-error [control]="form.controls.email" />
          </div>

          <div class="field">
            <label for="nickname">Nickname</label>
            <input id="nickname" class="input" formControlName="nickname" autocomplete="username" />
            <vx-field-error [control]="form.controls.nickname" patternMessage="3-20 letters, digits or underscore." />
          </div>

          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" autocomplete="new-password" />
            <vx-field-error [control]="form.controls.password" />
          </div>

          <div class="field">
            <label for="confirm">Confirm password</label>
            <input id="confirm" class="input" type="password" formControlName="confirmPassword" autocomplete="new-password" />
            <vx-field-error [control]="form.controls.confirmPassword" />
          </div>

          <div class="field">
            <label for="dob">Date of birth</label>
            <input id="dob" class="input" type="date" formControlName="dateOfBirth" [max]="maxBirthDate" />
            <vx-field-error [control]="form.controls.dateOfBirth" />
          </div>

          <div class="field">
            <label class="checkbox">
              <input type="checkbox" formControlName="isAdultConfirmed" />
              <span>I confirm that I am 18 years or older and understand that VLX points are virtual.</span>
            </label>
            <vx-field-error [control]="form.controls.isAdultConfirmed" />
          </div>

          <button class="btn btn-primary btn-block btn-lg" type="submit" [disabled]="submitting()">
            {{ submitting() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>

        <p class="muted switch">Already registered? <a routerLink="/login">Log in</a></p>
      </div>
    </div>
  `,
  styles: `
    .switch { margin: 16px 0 0; text-align: center; }
  `
})
export class RegisterPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);

  protected readonly submitting = signal(false);

  /** The date picker does not even offer dates that are too recent. */
  protected readonly maxBirthDate = (() => {
    const d = new Date();
    d.setFullYear(d.getFullYear() - 18);
    return d.toISOString().slice(0, 10);
  })();

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      nickname: ['', [Validators.required, Validators.pattern(NICKNAME_PATTERN)]],
      password: ['', [Validators.required, strongPassword, Validators.maxLength(100)]],
      confirmPassword: ['', Validators.required],
      dateOfBirth: ['', [Validators.required, minAge(18)]],
      isAdultConfirmed: [false, Validators.requiredTrue]
    },
    { validators: fieldsMatch('password', 'confirmPassword') }
  );

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { confirmPassword: _, ...request } = this.form.getRawValue();

    this.submitting.set(true);
    try {
      await this.auth.register(request);
      this.toasts.success('Account created. You received 1,000 VLX!');
      await this.router.navigateByUrl('/crash');
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.submitting.set(false);
    }
  }
}
