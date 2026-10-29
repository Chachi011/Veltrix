import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TitleStrategy, provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthStore } from './core/auth/auth.store';
import { TranslatedTitleStrategy } from './core/i18n/title-strategy';
import { PreferencesStore } from './core/state/preferences.store';
import { errorInterceptor } from './core/http/error.interceptor';
import { loadingInterceptor } from './core/http/loading.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // No zone.js: the UI updates when a signal it reads changes
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    // Order matters. Requests pass loading → error → auth; responses come back the other way, so the
    // error interceptor only sees a 401 AFTER the auth interceptor has tried to refresh and retry.
    provideHttpClient(withInterceptors([loadingInterceptor, errorInterceptor, authInterceptor])),
    // Before the first page renders: restore the login from the saved refresh token (if any)
    provideAppInitializer(() => inject(AuthStore).restoreSession()),
    // Before the first page renders: this browser's language (Georgian by default), so no English flashes first
    provideAppInitializer(() => inject(PreferencesStore).apply()),
    // Browser tab titles in the current language
    { provide: TitleStrategy, useClass: TranslatedTitleStrategy }
  ]
};
