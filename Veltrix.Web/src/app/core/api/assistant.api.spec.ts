import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SILENT_ERRORS, SKIP_LOADING } from '../http/http-context';
import { AssistantApi } from './assistant.api';

describe('AssistantApi', () => {
  let api: AssistantApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(AssistantApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads whether the assistant is configured', () => {
    let configured: boolean | undefined;
    api.status().subscribe((s) => (configured = s.configured));

    http.expectOne({ method: 'GET', url: '/api/assistant/status' }).flush({ configured: false });

    expect(configured).toBeFalse();
  });

  it('asks without the global error toast and loading bar: the drawer shows both itself', () => {
    api.ask('Hi').subscribe({ error: () => undefined });

    const request = http.expectOne({ method: 'POST', url: '/api/assistant/messages' });
    expect(request.request.body).toEqual({ message: 'Hi' });
    expect(request.request.context.get(SILENT_ERRORS)).toBeTrue();
    expect(request.request.context.get(SKIP_LOADING)).toBeTrue();
    request.flush({ status: 503, detail: 'The assistant is not configured.' }, { status: 503, statusText: 'Service Unavailable' });
  });
});
