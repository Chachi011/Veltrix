// The refresh token must survive a page reload, so it is stored in localStorage.
// Trade-off: an httpOnly cookie would hide it from JavaScript entirely, but the API returns it in the
// JSON body (simpler to test in Swagger). The access token, which actually opens the API, stays in memory.

const REFRESH_TOKEN_KEY = 'vx.refreshToken';

export function readRefreshToken(): string | null {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch {
    return null;
  }
}

export function saveRefreshToken(token: string): void {
  try {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  } catch {
    // Storage blocked: the session simply ends when the tab is closed
  }
}

export function removeRefreshToken(): void {
  try {
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  } catch {
    // Nothing to remove
  }
}
