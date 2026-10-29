import { isDevMode } from '@angular/core';
import { LucideIconData } from 'lucide-angular';
import { UserRole } from '../core/models/api.models';
import { mark } from '../core/i18n/i18n';
import { Icons } from '../shared/ui/icons';

export interface NavItem {
  /** English; shown through the t pipe. */
  label: string;
  /** Shorter label for the mobile bottom bar. */
  shortLabel: string;
  path: string;
  icon: LucideIconData;
  /** Only shown to logged-in users. */
  auth?: boolean;
  /** Only shown to these roles (the route guard and the server check the same roles). */
  roles?: UserRole[];
  /** Staff tools are grouped under their own heading in the sidebar. */
  section?: 'staff';
  /** Only in the account menu, not in the sidebar. */
  menuOnly?: boolean;
}

/** One list for the sidebar, the bottom nav and the account menu, so they can never disagree. */
export const NAV_ITEMS: NavItem[] = [
  { label: mark('Home'), shortLabel: mark('Home'), path: '/', icon: Icons.home },
  { label: mark('Play'), shortLabel: mark('Play'), path: '/crash', icon: Icons.rocket },
  { label: mark('Games'), shortLabel: mark('Games'), path: '/games', icon: Icons.gamepad },
  { label: mark('Leaderboard'), shortLabel: mark('Top'), path: '/leaderboard', icon: Icons.trophy },
  { label: mark('History'), shortLabel: mark('History'), path: '/history', icon: Icons.history, auth: true },
  { label: mark('Wallet'), shortLabel: mark('Wallet'), path: '/wallet', icon: Icons.wallet, auth: true },
  { label: mark('Responsible play'), shortLabel: mark('Limits'), path: '/limits', icon: Icons.responsible, roles: ['Player'] },
  { label: mark('Provably fair'), shortLabel: mark('Fair'), path: '/fairness', icon: Icons.shield },
  { label: mark('Rules'), shortLabel: mark('Rules'), path: '/rules', icon: Icons.book },
  { label: mark('Settings'), shortLabel: mark('Settings'), path: '/settings', icon: Icons.settings, auth: true, menuOnly: true },
  { label: mark('Operator'), shortLabel: mark('Operator'), path: '/operator', icon: Icons.monitor, roles: ['Operator', 'Admin'], section: 'staff' },
  { label: mark('Admin'), shortLabel: mark('Admin'), path: '/admin', icon: Icons.dashboard, roles: ['Admin'], section: 'staff' },
  ...(isDevMode() ? [{ label: mark('Styleguide'), shortLabel: mark('Styles'), path: '/styleguide', icon: Icons.palette }] : [])
];

/** The items a visitor with this role may see (null = guest). */
export function visibleNavItems(role: UserRole | null): NavItem[] {
  return NAV_ITEMS.filter((item) => {
    if (item.roles) {
      return role !== null && item.roles.includes(role);
    }
    return !item.auth || role !== null;
  });
}

/** The phone bottom bar has room for 5 items; the rest is in the account menu. */
export const BOTTOM_NAV_PATHS = {
  user: ['/', '/crash', '/leaderboard', '/wallet'],
  guest: ['/', '/crash', '/leaderboard', '/rules']
};
