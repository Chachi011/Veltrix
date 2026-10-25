import { ChangeDetectionStrategy, Component, inject, isDevMode, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth.api';
import { applyServerErrors } from '../../core/http/problem';
import { ToastStore } from '../../core/ui/toast.store';
import { FieldError } from '../../shared/field-error';
import { fieldsMatch, strongPassword } from '../../shared/validators';

/** Two steps: 1) request a 6-digit code by email, 2) enter the code and a new password. */
@Component({
  selector: 'vx-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink, FieldError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-narrow">
      <div class="card">
        <h1>Reset password</h1>

        @if (step() === 'request') {
          <p class="muted">Enter your email and we will send you a one-time 6-digit code.</p>
          <form [formGroup]="requestForm" (ngSubmit)="requestCode()" novalidate>
            <div class="field">
              <label for="email">Email</label>
              <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
              <vx-field-error [control]="requestForm.controls.email" />
            </div>
            <button class="btn btn-primary btn-block" type="submit" [disabled]="busy()">Send code</button>
          </form>
        } @else {
          <p class="muted">
            If <strong>{{ requestForm.controls.email.value }}</strong> is registered, a code is on its way.
            @if (devMode) {
              <br /><em>Development: the "email" is written to the API console.</em>
            }
          </p>
          <form [formGroup]="resetForm" (ngSubmit)="reset()" novalidate>
            <div class="field">
              <label for="code">6-digit code</label>
              <input id="code" class="input num" inputmode="numeric" maxlength="6" formControlName="code" autocomplete="one-time-code" />
              <vx-field-error [control]="resetForm.controls.code" patternMessage="Enter the 6 digits from the email." />
            </div>
            <div class="field">
              <label for="newPassword">New password</label>
              <input id="newPassword" class="input" type="password" formControlName="newPassword" autocomplete="new-password" />
              <vx-field-error [control]="resetForm.controls.newPassword" />
            </div>
            <div class="field">
              <label for="confirm">Confirm new password</label>
              <input id="confirm" class="input" type="password" formControlName="confirmPassword" autocomplete="new-password" />
              <vx-field-error [control]="resetForm.controls.confirmPassword" />
            </div>
            <button class="btn btn-primary btn-block" type="submit" [disabled]="busy()">Set new password</button>
            <button class="btn btn-ghost btn-block" type="button" (click)="step.set('request')">Use another email</button>
          </form>
        }

        <p class="muted back"><a routerLink="/login">Back to log in</a></p>
      </div>
    </div>
  `,
  styles: `
    .back { margin: 16px 0 0; text-align: center; }
    .btn-ghost { margin-top: 8px; }
  `
})
export class ForgotPasswordPage {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly step = signal<'request' | 'reset'>('request');
  protected readonly busy = signal(false);
  protected readonly devMode = isDevMode();

  protected readonly requestForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  protected readonly resetForm = this.fb.group(
    {
      code: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]],
      newPassword: ['', [Validators.required, strongPassword]],
      confirmPassword: ['', Validators.required]
    },
    { validators: fieldsMatch('newPassword', 'confirmPassword') }
  );

  protected async requestCode(): Promise<void> {
    if (this.requestForm.invalid) {
      this.requestForm.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    try {
      await firstValueFrom(this.api.forgotPassword(this.requestForm.getRawValue().email));
      this.step.set('reset');
    } catch (error) {
      applyServerErrors(this.requestForm, error);
    } finally {
      this.busy.set(false);
    }
  }

  protected async reset(): Promise<void> {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }
    const { code, newPassword } = this.resetForm.getRawValue();

    this.busy.set(true);
    try {
      await firstValueFrom(this.api.resetPassword({ email: this.requestForm.getRawValue().email, code, newPassword }));
      this.toasts.success('Password changed. Log in with your new password.');
      await this.router.navigateByUrl('/login');
    } catch (error) {
      applyServerErrors(this.resetForm, error);
    } finally {
      this.busy.set(false);
    }
  }
}
