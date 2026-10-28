import { CdkMenu, CdkMenuItem } from '@angular/cdk/menu';
import { ConnectedPosition, Overlay } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { ChangeDetectionStrategy, Component, Injector, computed, inject, output, viewChild } from '@angular/core';
import { NavigationStart, Router, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { Subscription, filter } from 'rxjs';
import { AuthStore } from '../core/auth/auth.store';
import { I18n } from '../core/i18n/i18n';
import { TranslatePipe } from '../core/i18n/i18n-pipes';
import { PreferencesStore } from '../core/state/preferences.store';
import { ThemeService } from '../core/ui/theme.service';
import { Avatar } from '../shared/ui/avatar';
import { Icons } from '../shared/ui/icons';
import { visibleNavItems } from './nav-items';
import { NotificationsPanel } from './notifications-panel';

/*
 * The two dropdowns of the top bar (account menu, notifications). They live in their own file, loaded on the
 * first click (and prefetched when the browser is idle), because they need the CDK Overlay and Menu modules:
 * about 75 kB that every visitor would otherwise download before seeing the first page.
 */

const MENU_ITEM =
  'flex h-9 w-full items-center gap-2.5 rounded-lg px-3 text-left text-sm text-text outline-none ' +
  'transition-colors duration-150 hover:bg-surface-2 focus:bg-surface-2 cursor-pointer';

/** Account menu: profile, wallet, history, the role's extra pages, theme, log out. Arrow keys move between items. */
@Component({
  selector: 'vx-account-menu',
  imports: [RouterLink, LucideAngularModule, CdkMenu, CdkMenuItem, Avatar, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (auth.user(); as user) {
      <div cdkMenu class="w-60 animate-pop-in rounded-card border border-border bg-surface p-1.5 shadow-pop" [attr.aria-label]="'Account menu' | t">
        <div class="flex items-center gap-3 px-3 py-2.5">
          <vx-avatar [src]="user.avatarUrl" [name]="user.nickname" size="md" />
          <div class="min-w-0">
            <p class="truncate text-sm font-semibold">{{ user.nickname }}</p>
            <p class="truncate text-xs text-muted">{{ user.email }}</p>
          </div>
        </div>
        <div class="my-1 h-px bg-border" role="separator"></div>
        <a cdkMenuItem routerLink="/profile" [class]="menuItem">
          <lucide-icon [img]="icons.user" [size]="16" class="text-muted" aria-hidden="true" /> {{ 'Profile' | t }}
        </a>
        <a cdkMenuItem routerLink="/wallet" [class]="menuItem">
          <lucide-icon [img]="icons.wallet" [size]="16" class="text-muted" aria-hidden="true" /> {{ 'Wallet' | t }}
        </a>
        <a cdkMenuItem routerLink="/history" [class]="menuItem">
          <lucide-icon [img]="icons.history" [size]="16" class="text-muted" aria-hidden="true" /> {{ 'History' | t }}
        </a>
        <!-- Role-dependent pages (on phones this menu is the only way to reach them) -->
        @for (item of extras(); track item.path) {
          <a cdkMenuItem [routerLink]="item.path" [class]="menuItem">
            <lucide-icon [img]="item.icon" [size]="16" class="text-muted" aria-hidden="true" /> {{ item.label | t }}
          </a>
        }
        <button cdkMenuItem type="button" [class]="menuItem" (cdkMenuItemTriggered)="preferences.toggleTheme(); closed.emit()">
          <lucide-icon [img]="theme.theme() === 'dark' ? icons.sun : icons.moon" [size]="16" class="text-muted" aria-hidden="true" />
          {{ theme.theme() === 'dark' ? ('Light theme' | t) : ('Dark theme' | t) }}
        </button>
        <button cdkMenuItem type="button" [class]="menuItem" (cdkMenuItemTriggered)="preferences.toggleLanguage(); closed.emit()">
          <lucide-icon [img]="icons.languages" [size]="16" class="text-muted" aria-hidden="true" />
          <!-- Each language is named in its own language, so it can be found by someone who cannot read the other -->
          {{ i18n.language() === 'ka' ? 'English' : 'ქართული' }}
        </button>
        <div class="my-1 h-px bg-border" role="separator"></div>
        <button cdkMenuItem type="button" [class]="menuItem + ' text-danger!'" (cdkMenuItemTriggered)="logout()">
          <lucide-icon [img]="icons.logout" [size]="16" aria-hidden="true" /> {{ 'Log out' | t }}
        </button>
      </div>
    }
  `
})
export class AccountMenu {
  protected readonly auth = inject(AuthStore);
  protected readonly theme = inject(ThemeService);
  protected readonly preferences = inject(PreferencesStore);
  protected readonly i18n = inject(I18n);
  protected readonly icons = Icons;
  protected readonly menuItem = MENU_ITEM;

  /** Emitted when the menu should close (an item was used). */
  readonly closed = output<void>();

  private readonly menu = viewChild(CdkMenu);

  /** Pages the account menu adds for this role (responsible play, staff panels, provably fair). */
  protected readonly extras = computed(() =>
    visibleNavItems(this.auth.role()).filter((item) =>
      ['/limits', '/games', '/fairness', '/rules', '/settings', '/operator', '/admin'].includes(item.path)
    )
  );

  focusFirstItem(): void {
    this.menu()?.focusFirstItem('keyboard');
  }

  protected logout(): void {
    this.closed.emit();
    void this.auth.logout();
  }
}

/** The bell dropdown: a dialog with the notifications panel. */
@Component({
  selector: 'vx-notifications-popup',
  imports: [NotificationsPanel, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-[360px] max-w-[calc(100vw-32px)] animate-pop-in overflow-hidden rounded-card border border-border bg-surface shadow-pop" role="dialog" [attr.aria-label]="'Notifications' | t">
      <vx-notifications-panel (closed)="closed.emit()" />
    </div>
  `
})
export class NotificationsPopup {
  readonly closed = output<void>();
}

export type TopBarPopupKind = 'account' | 'notifications';

export interface TopBarPopup {
  /** Closes the popup; `restoreFocus` puts the keyboard focus back on the button that opened it. */
  close(restoreFocus?: boolean): void;
}

const POSITIONS: ConnectedPosition[] = [
  { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 8 }
];

/**
 * Opens a dropdown under `origin`. It closes on Escape (focus returns to the button), on a click outside,
 * on navigation and when one of its items is used. `onClosed` runs once, whatever closed it.
 */
export function openTopBarPopup(kind: TopBarPopupKind, origin: HTMLElement, injector: Injector, onClosed: () => void): TopBarPopup {
  const overlay = injector.get(Overlay);
  const router = injector.get(Router);

  const overlayRef = overlay.create({
    positionStrategy: overlay.position().flexibleConnectedTo(origin).withPositions(POSITIONS).withViewportMargin(8),
    scrollStrategy: overlay.scrollStrategies.reposition()
  });

  const component =
    kind === 'account'
      ? overlayRef.attach(new ComponentPortal(AccountMenu, null, injector))
      : overlayRef.attach(new ComponentPortal(NotificationsPopup, null, injector));

  const subscriptions = new Subscription();
  let open = true;

  const close = (restoreFocus = false) => {
    if (!open) {
      return;
    }
    open = false;
    subscriptions.unsubscribe();
    overlayRef.dispose();
    onClosed();
    if (restoreFocus) {
      origin.focus();
    }
  };

  subscriptions.add(component.instance.closed.subscribe(() => close()));
  subscriptions.add(
    overlayRef.keydownEvents().subscribe((event) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        close(true);
      } else if (event.key === 'Tab' && kind === 'account') {
        close(); // a menu is left with Tab, like a native one
      }
    })
  );
  // A click on the button itself is handled by the button (it toggles)
  subscriptions.add(overlayRef.outsidePointerEvents().subscribe((event) => !origin.contains(event.target as Node) && close()));
  subscriptions.add(router.events.pipe(filter((e) => e instanceof NavigationStart)).subscribe(() => close()));

  // Render now (not on the next tick), so the menu's first item exists and can take the focus
  component.changeDetectorRef.detectChanges();
  if (component.instance instanceof AccountMenu) {
    component.instance.focusFirstItem();
  }

  return { close };
}
