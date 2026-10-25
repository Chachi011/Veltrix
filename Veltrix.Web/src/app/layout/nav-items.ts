import { isDevMode } from '@angular/core';
import { LucideIconData } from 'lucide-angular';
import { Icons } from '../shared/ui/icons';

export interface NavItem {
  label: string;
  /** Shorter label for the mobile bottom bar. */
  shortLabel: string;
  path: string;
  icon: LucideIconData;
  /** Only shown to logged-in users. */
  auth?: boolean;
}

/** One list for the sidebar and the bottom nav, so they can never disagree. */
export const NAV_ITEMS: NavItem[] = [
  { label: 'Play', shortLabel: 'Play', path: '/crash', icon: Icons.rocket },
  { label: 'History', shortLabel: 'History', path: '/history', icon: Icons.history, auth: true },
  { label: 'Wallet', shortLabel: 'Wallet', path: '/wallet', icon: Icons.wallet, auth: true },
  { label: 'Provably fair', shortLabel: 'Fair', path: '/fairness', icon: Icons.shield },
  ...(isDevMode() ? [{ label: 'Styleguide', shortLabel: 'Styles', path: '/styleguide', icon: Icons.palette }] : [])
];
