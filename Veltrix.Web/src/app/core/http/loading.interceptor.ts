import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingStore } from '../ui/loading.store';
import { SKIP_LOADING } from './http-context';

/** Counts requests in flight for the top progress bar. finalize() runs on success, error and cancel. */
export const loadingInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SKIP_LOADING)) {
    return next(request);
  }

  const loading = inject(LoadingStore);
  loading.start();
  return next(request).pipe(finalize(() => loading.stop()));
};
