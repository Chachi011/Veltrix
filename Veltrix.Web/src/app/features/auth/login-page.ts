import { ChangeDetectionStrategy, Component, inject, input, isDevMode, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { ToastStore } from '../../core/ui/toast.store';
import { FieldError } from '../../shared/field-error';
import { safeReturnUrl } from './safe-return-url';

@Component({
  selector: 'vx-login-page',
  imports: [ReactiveFormsModule, RouterLink, FieldError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-narrow">
      <div class="card">
        <h1>Log in</h1>
        <p class="muted">Welcome back. Play with virtual VLX points.</p>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
            <vx-field-error [control]="form.controls.email" />
          </div>

          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" autocomplete="current-password" />
            <vx-field-error [control]="form.controls.password" />
          </div>

          <p class="forgot"><a routerLink="/forgot-password">Forgot password?</a></p>

          <button class="btn btn-primary btn-block btn-lg" type="submit" [disabled]="submitting()">
            {{ submitting() ? 'Logging in…' : 'Log in' }}
          </button>
        </form>

        <p class="muted switch">No account yet? <a routerLink="/register">Register</a></p>

        @if (devMode) {
          <div class="dev">
            <span class="muted">Test accounts (development only):</span>
            <div class="row">
              @for (account of testAccounts; track account.email) {
                <button type="button" class="btn btn-sm" (click)="fill(account.email, account.password)">
                  {{ account.label }}
                </button>
              }
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: `
    .forgot { margin: -4px 0 16px; text-align: right; font-size: 0.9rem; }
    .switch { margin: 16px 0 0; text-align: center; }
    .dev { margin-top: 20px; padding-top: 16px; border-top: 1px dashed var(--border); font-size: 0.85rem; }
    .dev .row { margin-top: 8px; }
  `
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);

  /** From ?returnUrl=... (route query params are bound to inputs). */
  readonly returnUrl = input<string>();

  protected readonly submitting = signal(false);
  protected readonly devMode = isDevMode();

  protected readonly testAccounts = [
    { label: 'player1', email: 'player1@veltrix.dev', password: 'Player123!' },
    { label: 'player2', email: 'player2@veltrix.dev', password: 'Player123!' },
    { label: 'operator', email: 'operator@veltrix.dev', password: 'Operator123!' },
    { label: 'admin', email: 'admin@veltrix.dev', password: 'Admin123!' }
  ];

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  protected fill(email: string, password: string): void {
    this.form.setValue({ email, password });
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      await this.auth.login(this.form.getRawValue());
      this.toasts.success(`Welcome, ${this.auth.user()?.nickname}!`);
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.submitting.set(false);
    }
  }
}
