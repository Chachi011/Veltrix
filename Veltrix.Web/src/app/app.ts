import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth.store';
import { TranslatePipe } from './core/i18n/i18n-pipes';
import { CrashHub, UserHubHandlers } from './core/realtime/crash-hub';
import { LimitsStore } from './core/state/limits.store';
import { NotificationsStore } from './core/state/notifications.store';
import { PreferencesStore } from './core/state/preferences.store';
import { WalletStore } from './core/state/wallet.store';
import { LayoutService } from './core/ui/layout.service';
import { LoadingStore } from './core/ui/loading.store';
import { SessionReminder } from './features/limits/session-reminder';
import { BottomNav } from './layout/bottom-nav';
import { Sidebar } from './layout/sidebar';
import { TopBar } from './layout/top-bar';
import { ToastContainer } from './shared/ui/toast-container';

/** The shell: sidebar (desktop) or bottom nav (phone), glass top bar, the current page, toasts. */
@Component({
  selector: 'vx-root',
  imports: [RouterOutlet, Sidebar, TopBar, BottomNav, ToastContainer, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed top-0 left-0 z-50 h-0.5 bg-primary transition-[width,opacity] ease-out"
      [class]="loading.active() ? 'w-3/4 opacity-100 duration-[2000ms]' : 'w-0 opacity-0 duration-300'"
      aria-hidden="true"
    ></div>

    <a href="#main" class="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-[60] focus:rounded-control focus:bg-surface focus:px-4 focus:py-2">
      {{ 'Skip to content' | t }}
    </a>

    <vx-sidebar />

    <div class="min-h-screen transition-[padding] duration-200 ease-out-soft" [class]="contentOffset()">
      <vx-top-bar />
      <main id="main" class="mx-auto w-full max-w-[1440px] px-4 pt-6 pb-28 md:px-6 md:pb-12">
        <router-outlet />
      </main>
    </div>

    <vx-bottom-nav />
    <vx-toast-container />
  `
})
export class App {
  private readonly auth = inject(AuthStore);
  private readonly wallet = inject(WalletStore);
  private readonly notifications = inject(NotificationsStore);
  private readonly limits = inject(LimitsStore);
  /** Created here so theme, sound and reduced motion are applied from the very first page. */
  private readonly preferences = inject(PreferencesStore);
  private readonly hub = inject(CrashHub);
  private readonly layout = inject(LayoutService);
  protected readonly loading = inject(LoadingStore);

  /** Room for the fixed sidebar on desktop; none on phones. */
  protected readonly contentOffset = computed(() => (this.layout.sidebarCollapsed() ? 'md:pl-[72px]' : 'md:pl-60'));

  /** Only the user's id, so a nickname change does not reload the wallet. */
  private readonly userId = computed(() => this.auth.user()?.id ?? null);

  /** Personal real-time events, on every page while logged in. */
  private readonly userEvents: UserHubHandlers = {
    balanceUpdated: (e) => this.wallet.setBalance(e.balance),
    notification: (n) => void this.notifications.receive(n),
    // Events may have been missed while offline: reload the bell
    reconnected: () => void this.notifications.load().catch(() => undefined)
  };

  constructor() {
    // The session reminder runs its own timer from the moment it is created
    inject(SessionReminder);

    // Logged in → balance, notifications and the live connection; logged out → forget everything
    effect(() => {
      const userId = this.userId();
      untracked(() => {
        if (userId !== null) {
          this.wallet.load().catch(() => undefined);
          this.notifications.load().catch(() => undefined);
          // The account's settings (theme, sound, animations) follow the player to this device
          this.preferences.load().catch(() => undefined);
          void this.hub.startUserSession(this.userEvents);
        } else {
          this.wallet.reset();
          this.notifications.reset();
          void this.hub.endUserSession();
        }
      });
    });

    // Responsible play settings only exist for players (also re-checked after a role change)
    effect(() => {
      const isPlayer = this.auth.isPlayer();
      untracked(() => (isPlayer ? this.limits.load().catch(() => undefined) : this.limits.reset()));
    });
  }
}
