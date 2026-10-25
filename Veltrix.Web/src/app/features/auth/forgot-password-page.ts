import { ChangeDetectionStrategy, Component, inject, isDevMode, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../../core/api/auth.api';
import { applyServerErrors } from '../../core/http/problem';
import { ToastStore } from '../../core/ui/toast.store';
import { Button, Field, Icons, PasswordInput } from '../../shared/ui';
import { fieldsMatch, strongPassword } from '../../shared/validators';
import { AuthCard } from './auth-card';

/** Two steps: 1) request a 6-digit code by email, 2) enter the code and a new password. */
@Component({
  selector: 'vx-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule, AuthCard, Button, Field, PasswordInput],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-auth-card
      title="Reset your password"
      [subtitle]="step() === 'request' ? 'We will email you a one-time 6-digit code.' : 'Enter the code and choose a new password.'"
      [icon]="icons.key"
    >
      <!-- Step indicator -->
      <ol class="mb-5 flex items-center gap-2 text-xs font-medium" aria-label="Progress">
        <li class="flex items-center gap-2" [class]="step() === 'request' ? 'text-text' : 'text-muted'">
          <span class="grid size-5 place-items-center rounded-full text-[11px]" [class]="step() === 'request' ? 'bg-primary text-on-primary' : 'bg-success/15 text-success'">
            @if (step() === 'request') {
              1
            } @else {
              <lucide-icon [img]="icons.check" [size]="12" aria-hidden="true" />
            }
          </span>
          Email
        </li>
        <li class="h-px flex-1 bg-border" aria-hidden="true"></li>
        <li class="flex items-center gap-2" [class]="step() === 'reset' ? 'text-text' : 'text-muted'" [attr.aria-current]="step() === 'reset' ? 'step' : null">
          <span class="grid size-5 place-items-center rounded-full text-[11px]" [class]="step() === 'reset' ? 'bg-primary text-on-primary' : 'bg-surface-2 text-muted'">2</span>
          New password
        </li>
      </ol>

      @if (step() === 'request') {
        <form [formGroup]="requestForm" (ngSubmit)="requestCode()" novalidate class="flex flex-col gap-4">
          <vx-field label="Email" for="email" [control]="requestForm.controls.email">
            <div class="vx-input-wrap">
              <lucide-icon [img]="icons.mail" [size]="16" class="vx-input-icon" aria-hidden="true" />
              <input id="email" class="vx-input" type="email" formControlName="email" autocomplete="email" placeholder="you@example.com" />
            </div>
          </vx-field>
          <button vxButton type="submit" variant="primary" size="lg" block [loading]="busy()">Send code</button>
        </form>
      } @else {
        <div class="mb-4 flex gap-2.5 rounded-control border border-border bg-surface-2 p-3 text-[13px] text-muted">
          <lucide-icon [img]="icons.info" [size]="16" class="mt-0.5 shrink-0" aria-hidden="true" />
          <p>
            If <strong class="text-text">{{ requestForm.controls.email.value }}</strong> is registered, a code is on its way.
            @if (devMode) {
              Development: the email is written to the API console.
            }
          </p>
        </div>

        <form [formGroup]="resetForm" (ngSubmit)="reset()" novalidate class="flex flex-col gap-4">
          <vx-field label="6-digit code" for="code" [control]="resetForm.controls.code" patternMessage="Enter the 6 digits from the email.">
            <input
              id="code"
              class="vx-input num text-center text-lg tracking-[0.5em]"
              inputmode="numeric"
              maxlength="6"
              formControlName="code"
              autocomplete="one-time-code"
              placeholder="••••••"
            />
          </vx-field>
          <vx-field label="New password" for="newPassword" [control]="resetForm.controls.newPassword">
            <vx-password-input>
              <input id="newPassword" class="vx-input" type="password" formControlName="newPassword" autocomplete="new-password" />
            </vx-password-input>
          </vx-field>
          <vx-field label="Confirm new password" for="confirm" [control]="resetForm.controls.confirmPassword">
            <vx-password-input>
              <input id="confirm" class="vx-input" type="password" formControlName="confirmPassword" autocomplete="new-password" />
            </vx-password-input>
          </vx-field>
          <button vxButton type="submit" variant="primary" size="lg" block [loading]="busy()">Set new password</button>
          <button vxButton type="button" variant="ghost" block (click)="step.set('request')">Use another email</button>
        </form>
      }

      <p auth-footer><a routerLink="/login" class="font-medium">Back to log in</a></p>
    </vx-auth-card>
  `
})
export class ForgotPasswordPage {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icons = Icons;
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
