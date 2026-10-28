import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProfileApi } from '../api/profile.api';
import { AuthStore } from '../auth/auth.store';
import { I18n } from '../i18n/i18n';
import { UserPreferencesDto } from '../models/api.models';
import { ThemeService } from '../ui/theme.service';
import { PreferencesStore } from './preferences.store';

describe('PreferencesStore (settings: theme, sound, animations, language)', () => {
  let store: InstanceType<typeof PreferencesStore>;
  let api: jasmine.SpyObj<ProfileApi>;
  const loggedIn = signal(false);

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-motion');
    api = jasmine.createSpyObj<ProfileApi>('ProfileApi', ['preferences', 'updatePreferences']);
    api.updatePreferences.and.callFake((r) => of({ ...r, updatedAt: '2026-10-27T12:00:00Z' }));

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: ProfileApi, useValue: api },
        { provide: AuthStore, useValue: { isLoggedIn: loggedIn } }
      ]
    });
    store = TestBed.inject(PreferencesStore);
  });

  afterEach(() => document.documentElement.removeAttribute('data-motion'));

  it('turning animations off marks the page for reduced motion, and back on removes it', async () => {
    await store.update({ animationsEnabled: false });
    expect(document.documentElement.getAttribute('data-motion')).toBe('reduce');

    await store.update({ animationsEnabled: true });
    expect(document.documentElement.hasAttribute('data-motion')).toBeFalse();
  });

  it('a guest keeps the settings in this browser only', async () => {
    loggedIn.set(false);
    await store.update({ soundEnabled: false });

    expect(api.updatePreferences).not.toHaveBeenCalled();
    expect(JSON.parse(localStorage.getItem('vx.prefs')!)).toEqual({ soundEnabled: false, animationsEnabled: true, language: 'ka' });
  });

  it('a logged-in player saves every change on the account', async () => {
    loggedIn.set(true);
    await store.update({ theme: 'Light', soundEnabled: false });

    expect(api.updatePreferences).toHaveBeenCalledWith({ theme: 'Light', soundEnabled: false, animationsEnabled: true, language: 'ka' });
    expect(TestBed.inject(ThemeService).theme()).toBe('light');
  });

  it("after login the account's settings win and are applied", async () => {
    const saved: UserPreferencesDto = {
      theme: 'Dark',
      soundEnabled: false,
      animationsEnabled: false,
      language: 'en',
      updatedAt: '2026-10-27T12:00:00Z'
    };
    api.preferences.and.returnValue(of(saved));

    await store.load();

    expect(store.theme()).toBe('Dark');
    expect(store.soundEnabled()).toBeFalse();
    expect(TestBed.inject(ThemeService).theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-motion')).toBe('reduce');
    expect(store.language()).toBe('en');
    expect(TestBed.inject(I18n).language()).toBe('en');
  });

  it('Georgian is the default; the language switch applies at once and is saved on the account', async () => {
    const i18n = TestBed.inject(I18n);
    await store.apply();
    expect(store.language()).toBe('ka');
    expect(i18n.language()).toBe('ka');
    expect(i18n.t('Place bet')).toBe('ფსონის დადება');
    expect(document.documentElement.lang).toBe('ka');

    loggedIn.set(true);
    await store.toggleLanguage();
    expect(i18n.language()).toBe('en');
    expect(i18n.t('Place bet')).toBe('Place bet');
    expect(document.documentElement.lang).toBe('en');
    expect(api.updatePreferences).toHaveBeenCalledWith(jasmine.objectContaining({ language: 'en' }));
  });

  it('the sun / moon button switches to the other fixed theme', async () => {
    await store.update({ theme: 'Dark' });
    await store.toggleTheme();
    expect(store.theme()).toBe('Light');
    await store.toggleTheme();
    expect(store.theme()).toBe('Dark');
  });
});
