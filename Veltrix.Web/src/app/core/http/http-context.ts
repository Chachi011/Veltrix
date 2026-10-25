import { HttpContext, HttpContextToken, HttpParams } from '@angular/common/http';

/** Request flag: the caller shows the error itself, so the global error toast is skipped. */
export const SILENT_ERRORS = new HttpContextToken<boolean>(() => false);

/** Request flag: a background request that should not show the top loading bar. */
export const SKIP_LOADING = new HttpContextToken<boolean>(() => false);

export const silent = () => new HttpContext().set(SILENT_ERRORS, true);

/** Builds query-string params, leaving out null / undefined / empty values. */
export function toParams(values: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
