import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { applyServerErrors } from '../../core/http/problem';
import { I18n } from '../../core/i18n/i18n';
import { LocalDatePipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { SystemSettingsDto } from '../../core/models/api.models';
import { ToastStore } from '../../core/ui/toast.store';
import { Button, Card, Field, Icons, PageHeader, Skeleton } from '../../shared/ui';
import { twoDecimals } from '../../shared/validators';
import { AdminNav } from './admin-nav';

/** System settings (spec: starting balance, daily bonus amount). Default change detection: it shows form errors. */
@Component({
  selector: 'vx-admin-settings-page',
  imports: [LocalDatePipe, TranslatePipe, ReactiveFormsModule, LucideAngularModule, AdminNav, Button, Card, Field, PageHeader, Skeleton],
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <vx-page-header [title]="'Settings' | t" [description]="'Platform values an admin can change without a new deployment.' | t" />
    <vx-admin-nav />

    <div class="grid gap-4 md:gap-6 lg:grid-cols-3">
      <vx-card
        class="lg:col-span-2"
        [title]="'Virtual points' | t"
        [subtitle]="'Changes apply immediately: the server reads them on every registration and bonus claim.' | t"
      >
        @if (settings(); as s) {
          <form class="flex flex-col gap-4" [formGroup]="form" (ngSubmit)="save()" novalidate>
            <div class="grid gap-4 sm:grid-cols-2">
              <vx-field
                [label]="'Starting balance' | t"
                for="starting"
                [control]="form.controls.startingBalance"
                [hint]="'Given to every NEW account. Existing balances do not change.' | t"
              >
                <div class="vx-input-wrap">
                  <lucide-icon [img]="icons.vlx" [size]="16" class="vx-input-icon" aria-hidden="true" />
                  <input id="starting" class="vx-input num" type="number" inputmode="decimal" formControlName="startingBalance" />
                  <span class="vx-input-suffix">VLX</span>
                </div>
              </vx-field>
              <vx-field
                [label]="'Daily bonus' | t"
                for="bonus"
                [control]="form.controls.dailyBonusAmount"
                [hint]="'Claimable once every 24 hours.' | t"
              >
                <div class="vx-input-wrap">
                  <lucide-icon [img]="icons.gift" [size]="16" class="vx-input-icon" aria-hidden="true" />
                  <input id="bonus" class="vx-input num" type="number" inputmode="decimal" formControlName="dailyBonusAmount" />
                  <span class="vx-input-suffix">VLX</span>
                </div>
              </vx-field>
            </div>
            <div class="flex flex-wrap items-center justify-between gap-3">
              <p class="text-xs text-muted">{{ 'Last changed {date}' | t: { date: (s.updatedAt | vxDate: 'MMM d, y, HH:mm') } }}</p>
              <!-- Phones: stacked, full width, Save on top (the Georgian labels are too long for one row); wider: side by side -->
              <div class="flex w-full flex-col-reverse gap-2 sm:w-auto sm:flex-row">
                <button vxButton type="button" variant="ghost" [disabled]="form.pristine" (click)="reset()">{{ 'Discard' | t }}</button>
                <button vxButton type="submit" variant="primary" [loading]="saving()" [disabled]="form.pristine">{{ 'Save settings' | t }}</button>
              </div>
            </div>
          </form>
        } @else {
          <div class="grid gap-4 sm:grid-cols-2">
            <vx-skeleton height="4.5rem" shape="rect" />
            <vx-skeleton height="4.5rem" shape="rect" />
          </div>
        }
      </vx-card>

      <vx-card [title]="'Good to know' | t">
        <ul class="flex flex-col gap-3 text-[13px] text-muted">
          <li class="flex items-start gap-2">
            <lucide-icon [img]="icons.audit" [size]="15" class="mt-0.5 shrink-0 text-accent" aria-hidden="true" />
            {{ 'Each changed value is written to the audit log with its old and new value.' | t }}
          </li>
          <li class="flex items-start gap-2">
            <lucide-icon [img]="icons.shield" [size]="15" class="mt-0.5 shrink-0 text-accent" aria-hidden="true" />
            {{ 'VLX are virtual points: there is no deposit, purchase or withdrawal anywhere in the system.' | t }}
          </li>
        </ul>
      </vx-card>
    </div>
  `
})
export class AdminSettingsPage {
  private readonly api = inject(AdminApi);
  private readonly toasts = inject(ToastStore);
  private readonly i18n = inject(I18n);

  protected readonly icons = Icons;
  protected readonly settings = signal<SystemSettingsDto | null>(null);
  protected readonly saving = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    startingBalance: [0, [Validators.required, Validators.min(0), Validators.max(1_000_000), twoDecimals]],
    dailyBonusAmount: [0, [Validators.required, Validators.min(1), Validators.max(100_000), twoDecimals]]
  });

  constructor() {
    void firstValueFrom(this.api.settings())
      .then((s) => this.apply(s))
      .catch(() => undefined);
  }

  protected reset(): void {
    const s = this.settings();
    if (s) {
      this.apply(s);
    }
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { startingBalance, dailyBonusAmount } = this.form.getRawValue();
    this.saving.set(true);
    try {
      this.apply(await firstValueFrom(this.api.updateSettings(startingBalance, dailyBonusAmount)));
      this.toasts.success(this.i18n.t('Settings saved.'));
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.saving.set(false);
    }
  }

  private apply(settings: SystemSettingsDto): void {
    this.settings.set(settings);
    this.form.reset({ startingBalance: settings.startingBalance, dailyBonusAmount: settings.dailyBonusAmount });
  }
}
