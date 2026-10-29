import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthStore } from './auth.store';

/** Lets the interceptor's promises (getValidAccessToken, refresh) finish. */
const settle = () => new Promise<void>((resolve) => setTimeout(resolve));

/** The parts of AuthStore the interceptor uses. `refreshTo` = the token a refresh gives (null: refresh fails). */
function fakeAuth(token: string | null, refreshTo: string | null = null) {
  const state = { token };
  return {
    getValidAccessToken: jasmine.createSpy('getValidAccessToken').and.callFake(async () => state.token),
    isLoggedIn: () => token !== null,
    accessToken: () => state.token,
    refresh: jasmine.createSpy('refresh').and.callFake(async () => {
      if (refreshTo === null) {
        return false;
      }
      state.token = refreshTo;
      return true;
    }),
    sessionExpired: jasmine.createSpy('sessionExpired').and.resolveTo()
  };
}

describe('authInterceptor (access token + refresh on 401)', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  function setup(auth: ReturnType<typeof fakeAuth>) {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: auth }
      ]
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  }

  afterEach(() => backend.verify());

  it('adds the access token to API calls, but not to auth endpoints or other files', async () => {
    setup(fakeAuth('token-1'));

    const wallet = firstValueFrom(http.get('/api/wallet'));
    await settle();
    const walletRequest = backend.expectOne('/api/wallet');
    expect(walletRequest.request.headers.get('Authorization')).toBe('Bearer token-1');
    walletRequest.flush({ balance: 1000 });
    await wallet;

    const login = firstValueFrom(http.post('/api/auth/login', {}));
    const loginRequest = backend.expectOne('/api/auth/login');
    expect(loginRequest.request.headers.has('Authorization')).toBeFalse();
    loginRequest.flush({});
    await login;

    const asset = firstValueFrom(http.get('/assets/config.json'));
    const assetRequest = backend.expectOne('/assets/config.json');
    expect(assetRequest.request.headers.has('Authorization')).toBeFalse();
    assetRequest.flush({});
    await asset;
  });

  it('after a 401 refreshes ONCE and repeats the request with the new token', async () => {
    const auth = fakeAuth('old-token', 'new-token');
    setup(auth);

    const result = firstValueFrom(http.get<{ balance: number }>('/api/wallet'));
    await settle();
    backend.expectOne('/api/wallet').flush({}, { status: 401, statusText: 'Unauthorized' });
    await settle();

    const retry = backend.expectOne('/api/wallet');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush({ balance: 990 });

    expect((await result).balance).toBe(990);
    expect(auth.refresh).toHaveBeenCalledTimes(1);
    expect(auth.sessionExpired).not.toHaveBeenCalled();
  });

  it('ends the session when the refresh fails', async () => {
    const auth = fakeAuth('old-token', null);
    setup(auth);

    const result = firstValueFrom(http.get('/api/wallet'));
    await settle();
    backend.expectOne('/api/wallet').flush({}, { status: 401, statusText: 'Unauthorized' });

    await expectAsync(result).toBeRejected();
    expect(auth.sessionExpired).toHaveBeenCalledTimes(1);
  });

  it('does not try to refresh for guests or for errors other than 401', async () => {
    const guest = fakeAuth(null, 'never');
    setup(guest);

    const asGuest = firstValueFrom(http.get('/api/profile'));
    await settle();
    const guestRequest = backend.expectOne('/api/profile');
    expect(guestRequest.request.headers.has('Authorization')).toBeFalse();
    guestRequest.flush({}, { status: 401, statusText: 'Unauthorized' });
    await expectAsync(asGuest).toBeRejected();

    expect(guest.refresh).not.toHaveBeenCalled();
  });

  it('passes a 403 straight through (logged in, but not allowed)', async () => {
    const auth = fakeAuth('player-token', 'new-token');
    setup(auth);

    const result = firstValueFrom(http.get('/api/admin/users'));
    await settle();
    backend.expectOne('/api/admin/users').flush({ detail: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });

    await expectAsync(result).toBeRejected();
    expect(auth.refresh).not.toHaveBeenCalled();
  });
});
