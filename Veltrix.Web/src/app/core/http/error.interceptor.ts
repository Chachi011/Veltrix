import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { I18n } from '../i18n/i18n';
import { ToastStore } from '../ui/toast.store';
import { SILENT_ERRORS } from './http-context';
import { problemMessage, toProblem } from './problem';

/**
 * Shows every API error to the user in a readable form (the ProblemDetails "detail" sentence, in the current language).
 * The error is re-thrown, so the component can still react (e.g. mark form fields).
 * 401 is left to the auth interceptor, which refreshes or ends the session.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toasts = inject(ToastStore);
  const i18n = inject(I18n);

  return next(request).pipe(
    catchError((error: unknown) => {
      const problem = toProblem(error);
      if (!request.context.get(SILENT_ERRORS) && problem.status !== 401) {
        // The API speaks English; known messages are shown in Georgian when the UI is Georgian
        toasts.error(i18n.server(problemMessage(problem)));
      }
      return throwError(() => error);
    })
  );
};
