import { ChangeDetectionStrategy, Component, inject, input, isDevMode, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { I18n } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { ToastStore } from '../../core/ui/toast.store';
import { Avatar, Button, Field, Icons, PasswordInput } from '../../shared/ui';
import { AuthCard } from './auth-card';
import { safeReturnUrl } from './safe-return-url';

@Component({
  selector: 'vx-login-page',
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule, AuthCard, Avatar, Button, Field, PasswordInput, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-auth-card [title]="'Welcome back' | t" [subtitle]="'Log in to play with virtual VLX points.' | t" [icon]="icons.rocket">
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="flex flex-col gap-4">
        <vx-field [label]="'Email' | t" for="email" [control]="form.controls.email">
          <div class="vx-input-wrap">
            <lucide-icon [img]="icons.mail" [size]="16" class="vx-input-icon" aria-hidden="true" />
            <input id="email" class="vx-input" type="email" formControlName="email" autocomplete="email" placeholder="you@example.com" />
          </div>
        </vx-field>

        <vx-field [label]="'Password' | t" for="password" [control]="form.controls.password">
          <vx-password-input>
            <input id="password" class="vx-input" type="password" formControlName="password" autocomplete="current-password" />
          </vx-password-input>
        </vx-field>

        <div class="-mt-1 flex justify-end">
          <a routerLink="/forgot-password" class="text-[13px] font-medium">{{ 'Forgot password?' | t }}</a>
        </div>

        <button vxButton type="submit" variant="primary" size="lg" block [loading]="submitting()">{{ 'Log in' | t }}</button>
      </form>

      @if (devMode) {
        <div class="mt-6 border-t border-border pt-5">
          <p class="mb-3 text-xs font-medium text-muted">{{ 'Development · test accounts' | t }}</p>
          <div class="grid grid-cols-2 gap-2">
            @for (account of testAccounts; track account.email) {
              <button vxButton type="button" size="sm" class="justify-start" (click)="fill(account.email, account.password)">
                <vx-avatar [name]="account.label" size="xs" />{{ account.label }}
              </button>
            }
          </div>
        </div>
      }

      <p auth-footer>{{ 'No account yet?' | t }} <a routerLink="/register" class="font-medium">{{ 'Create one' | t }}</a></p>
    </vx-auth-card>
  `
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastStore);
  private readonly i18n = inject(I18n);

  /** From ?returnUrl=... (route query params are bound to inputs). */
  readonly returnUrl = input<string>();

  protected readonly icons = Icons;
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
      this.toasts.success(this.i18n.t('Welcome, {nickname}!', { nickname: this.auth.user()?.nickname ?? '' }));
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.submitting.set(false);
    }
  }
}
