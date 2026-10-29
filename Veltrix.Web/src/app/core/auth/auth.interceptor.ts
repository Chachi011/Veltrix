import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthStore } from './auth.store';

/** Login, register, refresh... must never carry (or try to refresh) an access token. */
const isAuthEndpoint = (url: string) => url.startsWith('/api/auth/');

const withToken = (request: HttpRequest<unknown>, token: string | null) =>
  token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

/**
 * 1. Adds "Authorization: Bearer <token>" to API calls (refreshing the token first if it is about to expire).
 * 2. If the API still answers 401, refreshes once and retries the request; if that fails, the session is over.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/') || isAuthEndpoint(request.url)) {
    return next(request);
  }

  const auth = inject(AuthStore);

  return from(auth.getValidAccessToken()).pipe(
    switchMap((token) => next(withToken(request, token))),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !auth.isLoggedIn()) {
        return throwError(() => error);
      }

      return from(auth.refresh()).pipe(
        switchMap((refreshed) => {
          if (!refreshed) {
            void auth.sessionExpired();
            return throwError(() => error);
          }
          return next(withToken(request, auth.accessToken()));
        })
      );
    })
  );
};
