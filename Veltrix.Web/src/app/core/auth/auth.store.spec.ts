import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthResponse } from '../models/api.models';
import { AuthStore } from './auth.store';
import { readRefreshToken, saveRefreshToken } from './token-storage';

/** A fake JWT whose payload says it lives `lifetimeSeconds` (only exp - iat is read by the store). */
function fakeJwt(lifetimeSeconds: number, id = 'a'): string {
  const encode = (o: object) => btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  const iat = Math.floor(Date.now() / 1000);
  return `${encode({ alg: 'HS256' })}.${encode({ sub: '1', iat, exp: iat + lifetimeSeconds, jti: id })}.signature`;
}

function authResponse(accessToken: string, refreshToken = 'refresh-1'): AuthResponse {
  return {
    accessToken,
    accessTokenExpiresAt: new Date(Date.now() + 900_000).toISOString(),
    refreshToken,
    refreshTokenExpiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(),
    user: { id: 1, email: 'player1@veltrix.dev', nickname: 'player1', role: 'Player', avatarUrl: null }
  };
}

describe('AuthStore', () => {
  let store: InstanceType<typeof AuthStore>;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    store = TestBed.inject(AuthStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify(); // no unexpected requests
    localStorage.clear();
  });

  it('login stores the user, keeps the access token in memory and the refresh token in storage', async () => {
    const login = store.login({ email: 'player1@veltrix.dev', password: 'Player123!' });
    http.expectOne('/api/auth/login').flush(authResponse(fakeJwt(900)));
    await login;

    expect(store.isLoggedIn()).toBeTrue();
    expect(store.isPlayer()).toBeTrue();
    expect(store.user()?.nickname).toBe('player1');
    expect(readRefreshToken()).toBe('refresh-1');
    expect(localStorage.getItem('vx.accessToken')).toBeNull();
  });

  it('getValidAccessToken returns the current token while it is fresh (no request)', async () => {
    const token = fakeJwt(900);
    const login = store.login({ email: 'a@b.cd', password: 'x' });
    http.expectOne('/api/auth/login').flush(authResponse(token));
    await login;

    expect(await store.getValidAccessToken()).toBe(token);
  });

  it('refreshes ONCE when several callers need a token that is about to expire', async () => {
    const login = store.login({ email: 'a@b.cd', password: 'x' });
    http.expectOne('/api/auth/login').flush(authResponse(fakeJwt(10, 'old'))); // 10 s left < 30 s margin
    await login;

    const first = store.getValidAccessToken();
    const second = store.getValidAccessToken();

    const refresh = http.expectOne('/api/auth/refresh'); // fails if there were two requests
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    const renewed = fakeJwt(900, 'new');
    refresh.flush(authResponse(renewed, 'refresh-2'));

    expect(await first).toBe(renewed);
    expect(await second).toBe(renewed);
    expect(readRefreshToken()).toBe('refresh-2'); // rotation: the old refresh token is replaced
  });

  it('a failed refresh ends the session', async () => {
    saveRefreshToken('revoked-token');
    const restore = store.restoreSession();
    http.expectOne('/api/auth/refresh').flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(store.isLoggedIn()).toBeFalse();
    expect(readRefreshToken()).toBeNull();
  });

  it('logout revokes the refresh token on the server and clears everything', async () => {
    const login = store.login({ email: 'a@b.cd', password: 'x' });
    http.expectOne('/api/auth/login').flush(authResponse(fakeJwt(900)));
    await login;
    const navigate = spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);

    const logout = store.logout();
    expect(store.isLoggedIn()).toBeFalse(); // cleared immediately, before the server answers
    const request = http.expectOne('/api/auth/logout');
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await logout;

    expect(readRefreshToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
