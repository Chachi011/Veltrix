import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../models/api.models';
import { AuthStore } from './auth.store';

// Guards only improve the user experience (no page that would just fail).
// The real protection is [Authorize] on the server: the API is always the final judge.

/** Logged-in users only; guests go to /login and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) =>
  inject(AuthStore).isLoggedIn()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

/** Login / register pages make no sense when already logged in. */
export const guestGuard: CanActivateFn = () =>
  inject(AuthStore).isLoggedIn() ? inject(Router).createUrlTree(['/crash']) : true;

/** Role-based pages (Operator / Admin panels in Phase 4). */
export const roleGuard =
  (...roles: UserRole[]): CanActivateFn =>
  (_route, state) => {
    const auth = inject(AuthStore);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    const role = auth.role();
    return role !== null && roles.includes(role) ? true : router.createUrlTree(['/crash']);
  };
