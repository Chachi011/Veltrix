import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth.store';
import { WalletStore } from './core/state/wallet.store';
import { LayoutService } from './core/ui/layout.service';
import { LoadingStore } from './core/ui/loading.store';
import { BottomNav } from './layout/bottom-nav';
import { Sidebar } from './layout/sidebar';
import { TopBar } from './layout/top-bar';
import { ToastContainer } from './shared/ui/toast-container';

/** The shell: sidebar (desktop) or bottom nav (phone), glass top bar, the current page, toasts. */
@Component({
  selector: 'vx-root',
  imports: [RouterOutlet, Sidebar, TopBar, BottomNav, ToastContainer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed top-0 left-0 z-50 h-0.5 bg-primary transition-[width,opacity] ease-out"
      [class]="loading.active() ? 'w-3/4 opacity-100 duration-[2000ms]' : 'w-0 opacity-0 duration-300'"
      aria-hidden="true"
    ></div>

    <a href="#main" class="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-[60] focus:rounded-control focus:bg-surface focus:px-4 focus:py-2">
      Skip to content
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
  private readonly layout = inject(LayoutService);
  protected readonly loading = inject(LoadingStore);

  /** Room for the fixed sidebar on desktop; none on phones. */
  protected readonly contentOffset = computed(() => (this.layout.sidebarCollapsed() ? 'md:pl-[72px]' : 'md:pl-60'));

  /** Only the user's id, so a nickname change does not reload the wallet. */
  private readonly userId = computed(() => this.auth.user()?.id ?? null);

  constructor() {
    // Logged in → load the balance for the top bar; logged out → forget it
    effect(() => {
      const userId = this.userId();
      untracked(() => {
        if (userId !== null) {
          this.wallet.load().catch(() => undefined);
        } else {
          this.wallet.reset();
        }
      });
    });
  }
}
