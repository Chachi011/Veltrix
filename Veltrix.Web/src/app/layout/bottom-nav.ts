import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { AuthStore } from '../core/auth/auth.store';
import { TranslatePipe } from '../core/i18n/i18n-pipes';
import { mark } from '../core/i18n/i18n';
import { Icons } from '../shared/ui/icons';
import { BOTTOM_NAV_PATHS, NAV_ITEMS } from './nav-items';

interface BottomItem {
  label: string;
  path: string;
  icon: LucideIconData;
}

/** Phone navigation: thumb-reachable bar at the bottom (below 768 px only). */
@Component({
  selector: 'vx-bottom-nav',
  imports: [RouterLink, RouterLinkActive, LucideAngularModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav
      class="glass fixed inset-x-0 bottom-0 z-40 border-t border-border pb-[env(safe-area-inset-bottom)] md:hidden"
      [attr.aria-label]="'Main navigation' | t"
    >
      <ul class="grid h-16" [style.grid-template-columns]="'repeat(' + items().length + ', minmax(0, 1fr))'">
        @for (item of items(); track item.path) {
          <li>
            <a
              [routerLink]="item.path"
              routerLinkActive
              #rla="routerLinkActive"
              [routerLinkActiveOptions]="{ exact: item.path === '/' }"
              [attr.aria-current]="rla.isActive ? 'page' : null"
              class="flex h-full flex-col items-center justify-center gap-1 text-[11px] font-medium transition-colors duration-200"
              [class]="rla.isActive ? 'text-primary-text' : 'text-muted'"
            >
              <lucide-icon [img]="item.icon" [size]="20" aria-hidden="true" />
              {{ item.label | t }}
            </a>
          </li>
        }
      </ul>
    </nav>
  `
})
export class BottomNav {
  private readonly auth = inject(AuthStore);

  protected readonly items = computed<BottomItem[]>(() => {
    const loggedIn = this.auth.isLoggedIn();
    const paths = loggedIn ? BOTTOM_NAV_PATHS.user : BOTTOM_NAV_PATHS.guest;
    const main = paths
      .map((path) => NAV_ITEMS.find((i) => i.path === path)!)
      .map((i) => ({ label: i.shortLabel, path: i.path, icon: i.icon }));

    // Everything else (responsible play, staff panels, provably fair) is in the account menu
    return loggedIn
      ? [...main, { label: mark('Profile'), path: '/profile', icon: Icons.user }]
      : [...main, { label: mark('Log in'), path: '/login', icon: Icons.user }];
  });
}
