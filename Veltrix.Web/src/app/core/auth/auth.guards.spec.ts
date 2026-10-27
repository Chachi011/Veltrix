import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { UserRole } from '../models/api.models';
import { roleGuard } from './auth.guards';
import { AuthStore } from './auth.store';

describe('roleGuard (route guards by role)', () => {
  const role = signal<UserRole | null>(null);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthStore, useValue: { isLoggedIn: () => role() !== null, role } }
      ]
    });
  });

  const run = (url: string, ...roles: UserRole[]) =>
    TestBed.runInInjectionContext(() =>
      roleGuard(...roles)({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot)
    ) as boolean | UrlTree;

  const serialize = (result: boolean | UrlTree) =>
    typeof result === 'boolean' ? result : TestBed.inject(Router).serializeUrl(result);

  it('lets the right roles in', () => {
    role.set('Admin');
    expect(run('/admin', 'Admin')).toBeTrue();
    expect(run('/operator', 'Operator', 'Admin')).toBeTrue();

    role.set('Operator');
    expect(run('/operator', 'Operator', 'Admin')).toBeTrue();

    role.set('Player');
    expect(run('/limits', 'Player')).toBeTrue();
  });

  it('sends other roles back to the game', () => {
    role.set('Player');
    expect(serialize(run('/admin', 'Admin'))).toBe('/crash');
    expect(serialize(run('/operator', 'Operator', 'Admin'))).toBe('/crash');

    role.set('Operator');
    expect(serialize(run('/admin', 'Admin'))).toBe('/crash');
    expect(serialize(run('/limits', 'Player'))).toBe('/crash');
  });

  it('sends guests to the login page and back afterwards', () => {
    role.set(null);
    expect(serialize(run('/admin/users', 'Admin'))).toBe('/login?returnUrl=%2Fadmin%2Fusers');
  });
});
