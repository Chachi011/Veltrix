import { isDevMode } from '@angular/core';
import { LucideIconData } from 'lucide-angular';
import { UserRole } from '../core/models/api.models';
import { Icons } from '../shared/ui/icons';

export interface NavItem {
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
  { label: 'Home', shortLabel: 'Home', path: '/', icon: Icons.home },
  { label: 'Play', shortLabel: 'Play', path: '/crash', icon: Icons.rocket },
  { label: 'Games', shortLabel: 'Games', path: '/games', icon: Icons.gamepad },
  { label: 'Leaderboard', shortLabel: 'Top', path: '/leaderboard', icon: Icons.trophy },
  { label: 'History', shortLabel: 'History', path: '/history', icon: Icons.history, auth: true },
  { label: 'Wallet', shortLabel: 'Wallet', path: '/wallet', icon: Icons.wallet, auth: true },
  { label: 'Responsible play', shortLabel: 'Limits', path: '/limits', icon: Icons.responsible, roles: ['Player'] },
  { label: 'Provably fair', shortLabel: 'Fair', path: '/fairness', icon: Icons.shield },
  { label: 'Rules', shortLabel: 'Rules', path: '/rules', icon: Icons.book },
  { label: 'Settings', shortLabel: 'Settings', path: '/settings', icon: Icons.settings, auth: true, menuOnly: true },
  { label: 'Operator', shortLabel: 'Operator', path: '/operator', icon: Icons.monitor, roles: ['Operator', 'Admin'], section: 'staff' },
  { label: 'Admin', shortLabel: 'Admin', path: '/admin', icon: Icons.dashboard, roles: ['Admin'], section: 'staff' },
  ...(isDevMode() ? [{ label: 'Styleguide', shortLabel: 'Styles', path: '/styleguide', icon: Icons.palette }] : [])
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
