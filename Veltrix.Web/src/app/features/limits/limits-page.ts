import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { SelfExclusionPeriod } from '../../core/models/api.models';
import { LimitsStore } from '../../core/state/limits.store';
import { ToastStore } from '../../core/ui/toast.store';
import { VlxPipe } from '../../shared/pipes';
import { Button, Card, Field, Icons, ModalService, PageHeader, ProgressBar, ProgressTone, Skeleton } from '../../shared/ui';
import { twoDecimals } from '../../shared/validators';

interface ExclusionOption {
  period: SelfExclusionPeriod;
  label: string;
  hours: number;
}

const EXCLUSIONS: ExclusionOption[] = [
  { period: 'Hours24', label: '24 hours', hours: 24 },
  { period: 'Days7', label: '7 days', hours: 7 * 24 },
  { period: 'Days30', label: '30 days', hours: 30 * 24 }
];

const REMINDER_OPTIONS = [
  { value: 0, label: 'Off' },
  { value: 15, label: 'Every 15 minutes' },
  { value: 30, label: 'Every 30 minutes' },
  { value: 60, label: 'Every 60 minutes' },
  { value: 90, label: 'Every 90 minutes' },
  { value: 120, label: 'Every 2 hours' }
];

const LIMIT_PRESETS = [50, 100, 250, 500];

/**
 * Responsible play: daily bet limit, session reminder, self-exclusion.
 * The server enforces every rule (lower limit = now, higher = after 24 h; exclusion cannot be shortened);
 * this page explains them before the player commits. Default change detection: it shows form errors.
 */
@Component({
  selector: 'vx-limits-page',
  imports: [DatePipe, ReactiveFormsModule, LucideAngularModule, VlxPipe, Button, Card, Field, PageHeader, ProgressBar, Skeleton],
  templateUrl: './limits-page.html',
  changeDetection: ChangeDetectionStrategy.Default
})
export class LimitsPage {
  protected readonly store = inject(LimitsStore);
  private readonly modal = inject(ModalService);
  private readonly toasts = inject(ToastStore);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icons = Icons;
  protected readonly exclusions = EXCLUSIONS;
  protected readonly reminderOptions = REMINDER_OPTIONS;
  protected readonly presets = LIMIT_PRESETS;
  protected readonly selected = signal<SelfExclusionPeriod>('Hours24');
  protected readonly selectedOption = computed(() => EXCLUSIONS.find((o) => o.period === this.selected())!);

  protected readonly limitForm = this.fb.group({
    enabled: [false],
    amount: this.fb.control<number | null>(null, [Validators.required, Validators.min(1), Validators.max(1_000_000), twoDecimals])
  });
  protected readonly reminder = this.fb.control(60);

  /** Ticks every 30 s for "resets in ..." and to re-check which exclusions are possible. */
  private readonly now = signal(Date.now());

  protected readonly usage = computed(() => {
    const l = this.store.limits();
    return l && l.dailyBetLimit ? Math.min(1, l.betToday / l.dailyBetLimit) : 0;
  });

  protected readonly usageTone = computed<ProgressTone>(() => {
    const u = this.usage();
    return u >= 1 ? 'danger' : u >= 0.7 ? 'warning' : 'primary';
  });

  protected readonly resetsIn = computed(() => {
    const l = this.store.limits();
    if (!l) {
      return '';
    }
    const minutes = Math.max(0, Math.round((Date.parse(l.dayResetsAt) - this.now()) / 60_000));
    return minutes >= 60 ? `${Math.floor(minutes / 60)} h ${minutes % 60} min` : `${minutes} min`;
  });

  constructor() {
    // Amount is only validated (and sent) when the switch is on
    this.limitForm.controls.enabled.valueChanges.pipe(takeUntilDestroyed()).subscribe((on) => {
      const amount = this.limitForm.controls.amount;
      if (on) {
        amount.enable({ emitEvent: false });
      } else {
        amount.disable({ emitEvent: false });
      }
    });

    // Show the saved values whenever they (re)load
    effect(() => {
      const l = this.store.limits();
      if (!l) {
        return;
      }
      this.limitForm.reset({ enabled: l.dailyBetLimit !== null, amount: l.dailyBetLimit });
      if (l.dailyBetLimit === null) {
        this.limitForm.controls.amount.disable({ emitEvent: false });
      }
      this.reminder.reset(l.sessionReminderMin);
    });

    void this.store.load().catch(() => undefined);

    const timer = setInterval(() => this.now.set(Date.now()), 30_000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected usePreset(amount: number): void {
    this.limitForm.controls.amount.setValue(amount);
    this.limitForm.controls.amount.markAsDirty();
  }

  protected async saveLimit(): Promise<void> {
    if (this.limitForm.invalid) {
      this.limitForm.markAllAsTouched();
      return;
    }
    const { enabled, amount } = this.limitForm.getRawValue();
    const before = this.store.limits();
    const ok = await this.store.update({
      dailyBetLimit: enabled ? amount : null,
      sessionReminderMin: before?.sessionReminderMin ?? 60
    });
    if (!ok) {
      return;
    }
    const after = this.store.limits();
    this.toasts.success(
      after?.hasPendingLimitChange
        ? `Saved. The higher limit starts in ${after.limitIncreaseDelayHours} hours.`
        : 'Daily limit saved. It applies right away.'
    );
  }

  protected async saveReminder(): Promise<void> {
    const l = this.store.limits();
    // Leave the daily limit as it is: sending the waiting value keeps its 24-hour timer running
    const dailyBetLimit = l?.hasPendingLimitChange ? l.pendingDailyBetLimit : (l?.dailyBetLimit ?? null);
    const ok = await this.store.update({ dailyBetLimit, sessionReminderMin: this.reminder.value });
    if (ok) {
      this.toasts.success(this.reminder.value === 0 ? 'Session reminder turned off.' : 'Session reminder saved.');
    }
  }

  /** An exclusion shorter than the current one would be refused by the server (it cannot be shortened). */
  protected isShorter(option: ExclusionOption): boolean {
    const until = this.store.selfExcludedUntil();
    return until !== null && Date.parse(until) >= this.now() + option.hours * 3_600_000;
  }

  protected async selfExclude(): Promise<void> {
    const option = this.selectedOption();
    const endsAt = new Date(Date.now() + option.hours * 3_600_000);
    const confirmed = await this.modal.confirm({
      title: `Pause betting for ${option.label}?`,
      message:
        `Until ${endsAt.toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' })}. ` +
        'You will not be able to place bets. This cannot be cancelled or shortened, not even by support.',
      confirmText: 'Pause betting',
      tone: 'danger'
    });
    if (confirmed && (await this.store.selfExclude(option.period))) {
      this.toasts.success(`Betting is paused for ${option.label}.`);
    }
  }
}
