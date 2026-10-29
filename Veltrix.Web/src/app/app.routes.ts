import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guards';

// Every page is lazy-loaded: its code is downloaded only when the user opens it.
export const routes: Routes = [
  {
    // Home (spec 4.7): games, the live round and a short leaderboard; public
    path: '',
    pathMatch: 'full',
    title: 'Home · VELTRIX',
    loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage)
  },
  {
    path: 'games',
    title: 'Games · VELTRIX',
    loadComponent: () => import('./features/games/games-page').then((m) => m.GamesPage)
  },
  {
    path: 'login',
    title: 'Log in · VELTRIX',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage)
  },
  {
    path: 'register',
    title: 'Register · VELTRIX',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage)
  },
  {
    path: 'forgot-password',
    title: 'Reset password · VELTRIX',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/forgot-password-page').then((m) => m.ForgotPasswordPage)
  },
  {
    // Public: guests may watch the live game
    path: 'crash',
    title: 'Crash · VELTRIX',
    loadComponent: () => import('./features/crash/crash-page').then((m) => m.CrashPage)
  },
  {
    // Any other Crash game from the catalog
    path: 'crash/:slug',
    title: 'Crash · VELTRIX',
    loadComponent: () => import('./features/crash/crash-page').then((m) => m.CrashPage)
  },
  {
    // Public player profile (spec 4.6)
    path: 'players/:nickname',
    title: 'Player · VELTRIX',
    loadComponent: () => import('./features/players/player-page').then((m) => m.PlayerPage)
  },
  {
    path: 'rules',
    title: 'Rules · VELTRIX',
    loadComponent: () => import('./features/rules/rules-page').then((m) => m.RulesPage)
  },
  {
    path: 'settings',
    title: 'Settings · VELTRIX',
    canActivate: [authGuard],
    loadComponent: () => import('./features/settings/settings-page').then((m) => m.SettingsPage)
  },
  {
    path: 'wallet',
    title: 'Wallet · VELTRIX',
    canActivate: [authGuard],
    loadComponent: () => import('./features/wallet/wallet-page').then((m) => m.WalletPage)
  },
  {
    path: 'history',
    title: 'History · VELTRIX',
    canActivate: [authGuard],
    loadComponent: () => import('./features/history/history-page').then((m) => m.HistoryPage)
  },
  {
    path: 'profile',
    title: 'Profile · VELTRIX',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage)
  },
  {
    path: 'fairness',
    title: 'Provably fair · VELTRIX',
    loadComponent: () => import('./features/fairness/fairness-page').then((m) => m.FairnessPage)
  },
  {
    // Public: guests may see the best players too
    path: 'leaderboard',
    title: 'Leaderboard · VELTRIX',
    loadComponent: () => import('./features/leaderboard/leaderboard-page').then((m) => m.LeaderboardPage)
  },
  {
    path: 'limits',
    title: 'Responsible play · VELTRIX',
    canActivate: [roleGuard('Player')],
    loadComponent: () => import('./features/limits/limits-page').then((m) => m.LimitsPage)
  },
  {
    path: 'operator',
    title: 'Operator · VELTRIX',
    canActivate: [roleGuard('Operator', 'Admin')],
    loadComponent: () => import('./features/operator/operator-page').then((m) => m.OperatorPage)
  },
  {
    // The admin panel: one guard for the whole section (a parent route without a component); each tab is its own lazy page
    path: 'admin',
    canActivate: [roleGuard('Admin')],
    children: [
      {
        path: '',
        title: 'Admin · VELTRIX',
        loadComponent: () => import('./features/admin/admin-dashboard-page').then((m) => m.AdminDashboardPage)
      },
      {
        path: 'users',
        title: 'Users · VELTRIX',
        loadComponent: () => import('./features/admin/admin-users-page').then((m) => m.AdminUsersPage)
      },
      {
        path: 'audit',
        title: 'Audit log · VELTRIX',
        loadComponent: () => import('./features/admin/admin-audit-page').then((m) => m.AdminAuditPage)
      },
      {
        path: 'settings',
        title: 'Settings · VELTRIX',
        loadComponent: () => import('./features/admin/admin-settings-page').then((m) => m.AdminSettingsPage)
      }
    ]
  },
  {
    // Living documentation of the design system (development builds only)
    path: 'styleguide',
    title: 'Styleguide · VELTRIX',
    canMatch: [() => isDevMode()],
    loadComponent: () => import('./features/styleguide/styleguide-page').then((m) => m.StyleguidePage)
  },
  {
    path: '**',
    title: 'Not found · VELTRIX',
    loadComponent: () => import('./features/not-found-page').then((m) => m.NotFoundPage)
  }
];
