import { ChangeDetectionStrategy, Component, DestroyRef, Injector, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { filter } from 'rxjs';
import { AuthStore } from '../core/auth/auth.store';
import { I18n } from '../core/i18n/i18n';
import { TranslatePipe } from '../core/i18n/i18n-pipes';
import { NotificationsStore } from '../core/state/notifications.store';
import { PreferencesStore } from '../core/state/preferences.store';
import { WalletStore } from '../core/state/wallet.store';
import { AnimatedNumber } from '../shared/ui/animated-number';
import { Avatar } from '../shared/ui/avatar';
import { Button } from '../shared/ui/button';
import { Icons } from '../shared/ui/icons';
import { ThemeToggle } from '../shared/ui/theme-toggle';
import type { TopBarPopup, TopBarPopupKind } from './top-bar-popups';

/** The dropdowns (and the CDK Overlay/Menu they need) load on first use, not with the first page. */
const loadPopups = () => import('./top-bar-popups');

/** The AI assistant drawer, its store and the CDK Dialog: only players who open it download them. */
const loadAssistant = () => import('../features/assistant/assistant-drawer');

/** Sticky glass bar: page title, balance, daily bonus, AI assistant, notifications, account menu. */
@Component({
  selector: 'vx-top-bar',
  imports: [RouterLink, LucideAngularModule, AnimatedNumber, Avatar, Button, ThemeToggle, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './top-bar.html'
})
export class TopBar {
  protected readonly auth = inject(AuthStore);
  protected readonly wallet = inject(WalletStore);
  protected readonly notifications = inject(NotificationsStore);
  protected readonly preferences = inject(PreferencesStore);
  protected readonly i18n = inject(I18n);
  protected readonly icons = Icons;

  private readonly injector = inject(Injector);

  /** Which dropdown is open (drives aria-expanded). */
  protected readonly openPopup = signal<TopBarPopupKind | null>(null);
  private popup: TopBarPopup | null = null;

  /** The assistant drawer is open (drives aria-expanded). */
  protected readonly assistantOpen = signal(false);

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
    const destroyRef = inject(DestroyRef);
    destroyRef.onDestroy(() => {
      clearInterval(timer);
      this.popup?.close();
    });

    // Logged in: fetch the dropdown code while the browser is idle, so the first click opens at once.
    // Logged out: nothing to show any more.
    effect(() => {
      if (this.auth.isLoggedIn()) {
        const idle = window.requestIdleCallback ?? ((callback: () => void) => setTimeout(callback, 2000));
        idle(() => void loadPopups().catch(() => undefined));
      } else {
        this.popup?.close();
      }
    });
  }

  protected claimBonus(): void {
    void this.wallet.claimDailyBonus();
  }

  protected async openAssistant(): Promise<void> {
    if (this.assistantOpen()) {
      return;
    }
    this.popup?.close();
    this.assistantOpen.set(true);
    const assistant = await loadAssistant().catch(() => null);
    if (!assistant) {
      this.assistantOpen.set(false); // offline: the code could not be downloaded
      return;
    }
    assistant.openAssistantDrawer(this.injector, () => this.assistantOpen.set(false));
  }

  /** Opens a dropdown under its button, or closes it when it is already open. */
  protected async toggle(kind: TopBarPopupKind, event: Event): Promise<void> {
    const button = event.currentTarget as HTMLElement;
    const wasOpen = this.openPopup() === kind;
    this.popup?.close();
    if (wasOpen) {
      return;
    }

    this.openPopup.set(kind);
    const popups = await loadPopups().catch(() => null);
    if (!popups) {
      this.openPopup.set(null); // offline and not prefetched yet: nothing to open
      return;
    }
    const { openTopBarPopup } = popups;
    if (this.openPopup() !== kind) {
      return; // closed (or switched) while the code was loading
    }
    this.popup = openTopBarPopup(kind, button, this.injector, () => {
      this.popup = null;
      this.openPopup.update((open) => (open === kind ? null : open));
    });
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
