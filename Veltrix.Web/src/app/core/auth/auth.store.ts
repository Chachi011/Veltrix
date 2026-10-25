import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { AuthResponse, LoginRequest, RegisterRequest, UserDto } from '../models/api.models';
import { readRefreshToken, removeRefreshToken, saveRefreshToken } from './token-storage';

/** Refresh the access token when less than this is left, instead of waiting for a 401. */
const EXPIRY_MARGIN_MS = 30_000;

type AuthState = {
  user: UserDto | null;
  /** Kept only in memory (not localStorage), so a script injected into the page cannot read it later. */
  accessToken: string | null;
  /** Local-clock timestamp (ms) when the access token expires. */
  accessTokenExpiresAt: number | null;
};

/**
 * Who is logged in, and the tokens. The one place that logs in, logs out and refreshes.
 * The UI (header, guards) reads the signals; the HTTP interceptor and SignalR ask for a valid token.
 */
export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>({ user: null, accessToken: null, accessTokenExpiresAt: null }),
  withComputed(({ user }) => ({
    isLoggedIn: computed(() => user() !== null),
    role: computed(() => user()?.role ?? null),
    isPlayer: computed(() => user()?.role === 'Player'),
    isStaff: computed(() => user()?.role === 'Operator' || user()?.role === 'Admin')
  })),
  withMethods((store, api = inject(AuthApi), router = inject(Router)) => {
    // Several requests can get a 401 at the same moment; they all wait for ONE refresh call
    let refreshInFlight: Promise<boolean> | null = null;

    const setSession = (response: AuthResponse) => {
      saveRefreshToken(response.refreshToken);
      patchState(store, {
        user: response.user,
        accessToken: response.accessToken,
        accessTokenExpiresAt: localExpiry(response.accessToken)
      });
    };

    const clearSession = () => {
      removeRefreshToken();
      patchState(store, { user: null, accessToken: null, accessTokenExpiresAt: null });
    };

    const refresh = (): Promise<boolean> => {
      if (refreshInFlight) {
        return refreshInFlight;
      }

      const refreshToken = readRefreshToken();
      if (!refreshToken) {
        return Promise.resolve(false);
      }

      refreshInFlight = firstValueFrom(api.refresh(refreshToken))
        .then((response) => {
          setSession(response);
          return true;
        })
        .catch(() => {
          clearSession();
          return false;
        })
        .finally(() => (refreshInFlight = null));

      return refreshInFlight;
    };

    return {
      async login(request: LoginRequest): Promise<void> {
        setSession(await firstValueFrom(api.login(request)));
      },

      async register(request: RegisterRequest): Promise<void> {
        setSession(await firstValueFrom(api.register(request)));
      },

      async logout(): Promise<void> {
        const refreshToken = readRefreshToken();
        clearSession();
        if (refreshToken) {
          // Revoke on the server too; if that fails the token still expires on its own
          await firstValueFrom(api.logout(refreshToken)).catch(() => undefined);
        }
        await router.navigateByUrl('/login');
      },

      /** Runs once at startup: a saved refresh token means "keep me logged in after a page reload". */
      async restoreSession(): Promise<void> {
        if (readRefreshToken()) {
          await refresh();
        }
      },

      refresh,

      /** A token that is valid for at least 30 more seconds, refreshing first if needed. Null for guests. */
      async getValidAccessToken(): Promise<string | null> {
        const token = store.accessToken();
        const expiresAt = store.accessTokenExpiresAt();

        if (token && expiresAt && expiresAt - Date.now() > EXPIRY_MARGIN_MS) {
          return token;
        }
        if (!store.isLoggedIn()) {
          return null;
        }
        return (await refresh()) ? store.accessToken() : null;
      },

      /** Called when the session cannot be renewed: back to the login page, then return here. */
      async sessionExpired(): Promise<void> {
        clearSession();
        await router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      },

      /** Keeps the header in sync after a profile change (nickname, avatar). */
      updateUser(changes: Partial<UserDto>): void {
        const user = store.user();
        if (user) {
          patchState(store, { user: { ...user, ...changes } });
        }
      }
    };
  })
);

/**
 * Expiry on THIS computer's clock: now + the token lifetime (exp - iat from the JWT payload).
 * Comparing the server's expiry time with a wrong local clock would break the session.
 */
function localExpiry(jwt: string): number {
  try {
    const payload = JSON.parse(atob(jwt.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    const lifetimeSeconds = Number(payload.exp) - Number(payload.iat);
    if (Number.isFinite(lifetimeSeconds) && lifetimeSeconds > 0) {
      return Date.now() + lifetimeSeconds * 1000;
    }
  } catch {
    // Malformed token: treat it as short-lived, the next request refreshes it
  }
  return Date.now() + 60_000;
}
