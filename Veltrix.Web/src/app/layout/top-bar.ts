import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { filter } from 'rxjs';
import { AuthStore } from '../core/auth/auth.store';
import { NotificationsStore } from '../core/state/notifications.store';
import { PreferencesStore } from '../core/state/preferences.store';
import { WalletStore } from '../core/state/wallet.store';
import { ThemeService } from '../core/ui/theme.service';
import { AnimatedNumber } from '../shared/ui/animated-number';
import { Avatar } from '../shared/ui/avatar';
import { Button } from '../shared/ui/button';
import { Icons } from '../shared/ui/icons';
import { ThemeToggle } from '../shared/ui/theme-toggle';
import { visibleNavItems } from './nav-items';
import { NotificationsPanel } from './notifications-panel';

const MENU_ITEM =
  'flex h-9 w-full items-center gap-2.5 rounded-lg px-3 text-left text-sm text-text outline-none ' +
  'transition-colors duration-150 hover:bg-surface-2 focus:bg-surface-2 cursor-pointer';

/** Sticky glass bar: page title, balance, daily bonus, notifications, account menu. */
@Component({
  selector: 'vx-top-bar',
  imports: [
    RouterLink,
    LucideAngularModule,
    CdkMenu,
    CdkMenuItem,
    CdkMenuTrigger,
    CdkConnectedOverlay,
    CdkOverlayOrigin,
    AnimatedNumber,
    Avatar,
    Button,
    ThemeToggle,
    NotificationsPanel
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './top-bar.html'
})
export class TopBar {
  protected readonly auth = inject(AuthStore);
  protected readonly wallet = inject(WalletStore);
  protected readonly notifications = inject(NotificationsStore);
  protected readonly theme = inject(ThemeService);
  protected readonly preferences = inject(PreferencesStore);
  protected readonly icons = Icons;
  protected readonly menuItem = MENU_ITEM;

  protected readonly notificationsOpen = signal(false);
  protected readonly notificationPositions: ConnectedPosition[] = [
    { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 8 }
  ];

  protected readonly pageTitle = signal('');

  /** Ticks every second for the daily-bonus countdown. */
  private readonly now = signal(Date.now());

  protected readonly bonusCountdown = computed(() => {
    const next = this.wallet.wallet()?.nextDailyBonusAt;
    const ms = next ? Date.parse(next) - this.now() : 0;
    if (ms <= 0) {
      return null;
    }
    const h = Math.floor(ms / 3_600_000);
    const m = Math.floor((ms % 3_600_000) / 60_000);
    const s = Math.floor((ms % 60_000) / 1000);
    return `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

  /** Pages the account menu adds for this role (responsible play, staff panels, provably fair). */
  protected readonly menuExtras = computed(() =>
    visibleNavItems(this.auth.role()).filter((item) => ['/limits', '/games', '/fairness', '/rules', '/settings', '/operator', '/admin'].includes(item.path))
  );

  protected readonly canClaimBonus = computed(() => this.wallet.wallet() !== null && this.bonusCountdown() === null);

  constructor() {
    const router = inject(Router);
    router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.pageTitle.set(deepestTitle(router.routerState.snapshot.root)));

    const timer = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected claimBonus(): void {
    void this.wallet.claimDailyBonus();
  }

  /** Escape closes the bell dropdown (the CDK menu does this itself; a plain overlay does not). */
  protected onOverlayKey(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.notificationsOpen.set(false);
    }
  }

  protected logout(): void {
    void this.auth.logout();
  }
}

/** "Wallet · VELTRIX" → "Wallet" */
function deepestTitle(route: ActivatedRouteSnapshot): string {
  let current = route;
  while (current.firstChild) {
    current = current.firstChild;
  }
  return (current.title ?? '').split(' · ')[0];
}
