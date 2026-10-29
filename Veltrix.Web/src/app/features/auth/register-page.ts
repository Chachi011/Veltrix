import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { I18n } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { ToastStore } from '../../core/ui/toast.store';
import { Button, Field, Icons, PasswordInput } from '../../shared/ui';
import { NICKNAME_PATTERN, fieldsMatch, minAge, strongPassword } from '../../shared/validators';
import { AuthCard } from './auth-card';

@Component({
  selector: 'vx-register-page',
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule, AuthCard, Button, Field, PasswordInput, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-auth-card [title]="'Create your account' | t" [subtitle]="'Start with 1,000 virtual VLX points. No real money, ever.' | t" [icon]="icons.user">
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="flex flex-col gap-4">
        <vx-field [label]="'Email' | t" for="email" [control]="form.controls.email">
          <div class="vx-input-wrap">
            <lucide-icon [img]="icons.mail" [size]="16" class="vx-input-icon" aria-hidden="true" />
            <input id="email" class="vx-input" type="email" formControlName="email" autocomplete="email" placeholder="you@example.com" />
          </div>
        </vx-field>

        <vx-field
          [label]="'Nickname' | t"
          for="nickname"
          [control]="form.controls.nickname"
          [hint]="'3-20 letters, digits or underscore. Shown to other players.' | t"
          [patternMessage]="'3-20 letters, digits or underscore.' | t"
        >
          <div class="vx-input-wrap">
            <lucide-icon [img]="icons.user" [size]="16" class="vx-input-icon" aria-hidden="true" />
            <input id="nickname" class="vx-input" formControlName="nickname" autocomplete="username" />
          </div>
        </vx-field>

        <vx-field [label]="'Password' | t" for="password" [control]="form.controls.password" [hint]="'8+ characters with upper- and lowercase letters and a digit.' | t">
          <vx-password-input>
            <input id="password" class="vx-input" type="password" formControlName="password" autocomplete="new-password" />
          </vx-password-input>
        </vx-field>

        <vx-field [label]="'Confirm password' | t" for="confirm" [control]="form.controls.confirmPassword">
          <vx-password-input>
            <input id="confirm" class="vx-input" type="password" formControlName="confirmPassword" autocomplete="new-password" />
          </vx-password-input>
        </vx-field>

        <vx-field [label]="'Date of birth' | t" for="dob" [control]="form.controls.dateOfBirth">
          <input id="dob" class="vx-input" type="date" formControlName="dateOfBirth" [max]="maxBirthDate" />
        </vx-field>

        <div class="flex flex-col gap-1.5">
          <label class="vx-checkbox">
            <input type="checkbox" formControlName="isAdultConfirmed" />
            <span>{{ 'I confirm that I am 18 years or older and understand that VLX points are virtual.' | t }}</span>
          </label>
          @if (adultError(); as message) {
            <p class="flex items-center gap-1.5 text-xs text-danger" role="alert">
              <lucide-icon [img]="icons.alert" [size]="13" aria-hidden="true" />{{ message }}
            </p>
          }
        </div>

        <button vxButton type="submit" variant="primary" size="lg" block [loading]="submitting()">{{ 'Create account' | t }}</button>
      </form>

      <p auth-footer>{{ 'Already registered?' | t }} <a routerLink="/login" class="font-medium">{{ 'Log in' | t }}</a></p>
    </vx-auth-card>
  `
})
export class RegisterPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);
  private readonly i18n = inject(I18n);

  protected readonly icons = Icons;
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

  protected adultError(): string | null {
    const control = this.form.controls.isAdultConfirmed;
    return control.touched && control.invalid
      ? control.errors?.['server']
        ? this.i18n.server(control.errors['server'])
        : this.i18n.t('You must confirm that you are 18 or older.')
      : null;
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { confirmPassword: _, ...request } = this.form.getRawValue();

    this.submitting.set(true);
    try {
      await this.auth.register(request);
      this.toasts.success(this.i18n.t('Account created. You received 1,000 VLX!'));
      await this.router.navigateByUrl('/crash');
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.submitting.set(false);
    }
  }
}
