import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { LoadingStore } from '../ui/loading.store';
import { ToastStore } from '../ui/toast.store';
import { errorInterceptor } from './error.interceptor';
import { SILENT_ERRORS, SKIP_LOADING } from './http-context';
import { loadingInterceptor } from './loading.interceptor';

describe('HTTP interceptors (loading → error)', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let toasts: { success: jasmine.Spy; error: jasmine.Spy; info: jasmine.Spy };

  beforeEach(() => {
    toasts = jasmine.createSpyObj('ToastStore', ['success', 'error', 'info']);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([loadingInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastStore, useValue: toasts }
      ]
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('shows the ProblemDetails sentence of a failed request', async () => {
    const request = firstValueFrom(http.post('/api/bets', {}));
    backend.expectOne('/api/bets').flush(
      { title: 'Request rejected', detail: 'Betting is closed for this round.', status: 400 },
      { status: 400, statusText: 'Bad Request' }
    );

    await expectAsync(request).toBeRejected();
    expect(toasts.error).toHaveBeenCalledWith('Betting is closed for this round.');
  });

  it('shows the first field error when there is no detail (validation)', async () => {
    const request = firstValueFrom(http.post('/api/limits', {}));
    backend.expectOne('/api/limits').flush(
      { title: 'Validation failed', status: 400, errors: { dailyBetLimit: ['The limit must be at least 1.'] } },
      { status: 400, statusText: 'Bad Request' }
    );

    await expectAsync(request).toBeRejected();
    expect(toasts.error).toHaveBeenCalledWith('The limit must be at least 1.');
  });

  it('stays quiet for 401 (the auth interceptor refreshes) and for requests marked silent', async () => {
    const unauthorized = firstValueFrom(http.get('/api/wallet'));
    backend.expectOne('/api/wallet').flush({}, { status: 401, statusText: 'Unauthorized' });
    await expectAsync(unauthorized).toBeRejected();

    const silent = firstValueFrom(http.get('/api/players/nobody', { context: new HttpContext().set(SILENT_ERRORS, true) }));
    backend.expectOne('/api/players/nobody').flush({ detail: 'Not found' }, { status: 404, statusText: 'Not Found' });
    await expectAsync(silent).toBeRejected();

    expect(toasts.error).not.toHaveBeenCalled();
  });

  it('counts requests in flight for the progress bar, but not background ones', async () => {
    const loading = TestBed.inject(LoadingStore);

    const normal = firstValueFrom(http.get('/api/games'));
    const background = firstValueFrom(http.get('/api/notifications', { context: new HttpContext().set(SKIP_LOADING, true) }));
    expect(loading.pending()).toBe(1);

    backend.expectOne('/api/games').flush([]);
    backend.expectOne('/api/notifications').flush({});
    await Promise.all([normal, background]);
    expect(loading.active()).toBeFalse();
  });
});
