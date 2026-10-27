import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { ToastStore } from '../../core/ui/toast.store';
import {
  AnimatedNumber,
  Avatar,
  Badge,
  BadgeTone,
  Button,
  ButtonSize,
  ButtonVariant,
  Card,
  ChartSeries,
  ChartView,
  DrawerShell,
  EmptyState,
  Field,
  HashBadge,
  Icons,
  ModalService,
  MultiplierPill,
  Pagination,
  PasswordInput,
  ProgressBar,
  ProgressRing,
  ProgressTone,
  Skeleton,
  StatCard,
  TabItem,
  Tabs
} from '../../shared/ui';

/** Demo content for the drawer. */
@Component({
  selector: 'vx-styleguide-drawer-demo',
  imports: [DrawerShell, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-drawer title="player1" description="player1@veltrix.dev">
      <p class="text-sm text-muted">The body scrolls; the header stays. Escape, the close button or a click on the backdrop closes it.</p>
      <div drawer-footer class="flex justify-end"><button vxButton type="button" variant="primary" size="sm">Save</button></div>
    </vx-drawer>
  `
})
class DrawerDemo {}

/**
 * Living documentation of the design system: every component, every variant, in both themes.
 * The same template is rendered twice, inside data-theme="dark" and data-theme="light" wrappers.
 */
@Component({
  selector: 'vx-styleguide-page',
  imports: [
    NgTemplateOutlet,
    ReactiveFormsModule,
    LucideAngularModule,
    AnimatedNumber,
    Avatar,
    Badge,
    Button,
    Card,
    ChartView,
    EmptyState,
    Field,
    HashBadge,
    MultiplierPill,
    Pagination,
    PasswordInput,
    ProgressBar,
    ProgressRing,
    Skeleton,
    StatCard,
    Tabs
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './styleguide-page.html'
})
export class StyleguidePage {
  private readonly toasts = inject(ToastStore);
  private readonly modal = inject(ModalService);

  protected readonly icons = Icons;
  protected readonly themes = ['dark', 'light'] as const;

  protected readonly colorTokens = [
    { name: 'bg', css: 'var(--bg)' },
    { name: 'surface', css: 'var(--surface)' },
    { name: 'surface-2', css: 'var(--surface-2)' },
    { name: 'border', css: 'var(--border)' },
    { name: 'text', css: 'var(--text)' },
    { name: 'text-muted', css: 'var(--text-muted)' },
    { name: 'primary', css: 'var(--primary)' },
    { name: 'accent', css: 'var(--accent)' },
    { name: 'success', css: 'var(--success)' },
    { name: 'danger', css: 'var(--danger)' },
    { name: 'warning', css: 'var(--warning)' }
  ];

  protected readonly variants: ButtonVariant[] = ['primary', 'secondary', 'outline', 'ghost', 'success', 'danger'];
  protected readonly sizes: ButtonSize[] = ['sm', 'md', 'lg', 'xl'];
  protected readonly tones: BadgeTone[] = ['neutral', 'primary', 'accent', 'success', 'danger', 'warning'];
  protected readonly pills = [1.0, 1.45, 1.98, 2.1, 5.75, 9.99, 12.4, 152.0];
  protected readonly hash = 'a9030b56e34e04fccead154a500e6ef138a903837bd9f066196d08bde5c1e574';

  protected readonly bets = [
    { nickname: 'player1', avatar: null, amount: 50, multiplier: 2.4, payout: 120 },
    { nickname: 'neo_x', avatar: null, amount: 250, multiplier: null, payout: null },
    { nickname: 'luna', avatar: null, amount: 15.5, multiplier: 1.35, payout: 20.92 },
    { nickname: 'kaito_77', avatar: null, amount: 1000, multiplier: null, payout: null }
  ];

  // ---- Interactive demos ----
  protected readonly tabItems: TabItem[] = [
    { value: 'bets', label: 'My bets' },
    { value: 'rounds', label: 'All rounds' },
    { value: 'stats', label: 'Statistics' }
  ];
  protected readonly tab = signal('bets');
  protected readonly chartDays = ['Oct 19', 'Oct 20', 'Oct 21', 'Oct 22', 'Oct 23', 'Oct 24', 'Oct 25'];
  protected readonly chartSeries: ChartSeries[] = [
    { label: 'Cashed out', data: [4, 6, 3, 8, 5, 9, 7], tone: 'success' },
    { label: 'Lost', data: [3, 2, 5, 4, 6, 3, 4], tone: 'danger' },
    { label: 'Net (VLX)', data: [12, 30, -18, 44, -6, 52, 20], tone: 'accent', kind: 'line', axis: 'right' }
  ];
  protected readonly doughnutSeries: ChartSeries[] = [{ label: 'Bets', data: [62, 38], tone: 'success', tones: ['success', 'danger'] }];
  protected readonly progressTones: ProgressTone[] = ['primary', 'accent', 'success', 'warning', 'danger'];
  protected readonly page = signal(2);
  protected readonly balance = signal(1093.4);
  protected readonly buttonLoading = signal(false);
  protected readonly crashed = signal(false);
  protected readonly countdown = signal(10);
  protected readonly ringValue = computed(() => this.countdown() / 10);

  protected readonly form = new FormGroup({
    email: new FormControl('player1@veltrix.dev', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    amount: new FormControl(25, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    nickname: new FormControl('x', { nonNullable: true, validators: [Validators.pattern(/^[A-Za-z0-9_]{3,20}$/)] }),
    status: new FormControl('all', { nonNullable: true })
  });

  constructor() {
    // Show the error state without the user having to type first
    this.form.controls.nickname.markAsTouched();

    // Countdown demo: 10 → 0, then again
    const timer = setInterval(() => this.countdown.update((v) => (v <= 0.1 ? 10 : Math.round((v - 0.1) * 10) / 10)), 100);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected changeBalance(delta: number): void {
    this.balance.update((b) => Math.max(0, Math.round((b + delta) * 100) / 100));
  }

  protected simulateLoading(): void {
    this.buttonLoading.set(true);
    setTimeout(() => this.buttonLoading.set(false), 1500);
  }

  protected toggleCrash(): void {
    // Remove and re-add the class, so the shake animation can play again
    this.crashed.set(false);
    requestAnimationFrame(() => this.crashed.set(true));
  }

  protected toast(kind: 'success' | 'error' | 'info'): void {
    const messages = {
      success: 'Cashed out at 2.34x: +23.40 VLX',
      error: 'Too late: the round has already crashed.',
      info: 'The round was cancelled. Your bet was refunded.'
    };
    this.toasts[kind](messages[kind]);
  }

  protected openDrawer(): void {
    this.modal.openDrawer(DrawerDemo, { ariaLabel: 'Demo drawer', width: '420px' });
  }

  protected async openModal(): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'Cancel round #128?',
      message: 'All active bets will be refunded automatically. This cannot be undone.',
      confirmText: 'Cancel round',
      cancelText: 'Keep running',
      tone: 'danger'
    });
    this.toasts.info(confirmed ? 'Confirmed.' : 'Dismissed.');
  }
}
