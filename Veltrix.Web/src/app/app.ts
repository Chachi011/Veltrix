import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthStore } from './core/auth/auth.store';
import { WalletStore } from './core/state/wallet.store';
import { LoadingStore } from './core/ui/loading.store';
import { ThemeService } from './core/ui/theme.service';
import { VlxPipe } from './shared/pipes';
import { ToastContainer } from './shared/toast-container';

/** The shell: header + current page + toasts. */
@Component({
  selector: 'vx-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainer, VlxPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  protected readonly auth = inject(AuthStore);
  protected readonly wallet = inject(WalletStore);
  protected readonly loading = inject(LoadingStore);
  protected readonly theme = inject(ThemeService);

  protected readonly menuOpen = signal(false);
  protected readonly initial = computed(() => this.auth.user()?.nickname.charAt(0).toUpperCase() ?? '?');

  /** Only the user's id, so a nickname change does not reload the wallet. */
  private readonly userId = computed(() => this.auth.user()?.id ?? null);

  constructor() {
    // Logged in → load the balance for the header; logged out → forget it
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

    // Close the mobile menu after every navigation
    inject(Router)
      .events.pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.menuOpen.set(false));
  }

  protected logout(): void {
    void this.auth.logout();
  }
}
