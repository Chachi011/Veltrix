import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { MOTION_ATTRIBUTE } from '../../shared/motion';
import { ProfileApi } from '../api/profile.api';
import { AuthStore } from '../auth/auth.store';
import { ThemePreference, UpdatePreferencesRequest } from '../models/api.models';
import { ThemeMode, ThemeService } from '../ui/theme.service';

const STORAGE_KEY = 'vx.prefs';

type PreferencesState = UpdatePreferencesRequest & { saving: boolean };

const toMode = (theme: ThemePreference): ThemeMode => (theme === 'System' ? 'system' : theme === 'Dark' ? 'dark' : 'light');
const toPreference = (mode: ThemeMode): ThemePreference => (mode === 'system' ? 'System' : mode === 'dark' ? 'Dark' : 'Light');

function readLocal(): Pick<UpdatePreferencesRequest, 'soundEnabled' | 'animationsEnabled'> {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}');
    return { soundEnabled: saved.soundEnabled !== false, animationsEnabled: saved.animationsEnabled !== false };
  } catch {
    return { soundEnabled: true, animationsEnabled: true };
  }
}

/**
 * The player's settings (spec 4.7): theme, game sounds, animations.
 * Logged in: saved on the account (GET/PUT /api/profile/preferences), so they follow the player to any device.
 * Always also kept in this browser, so they apply at once on the next visit (and for guests).
 */
export const PreferencesStore = signalStore(
  { providedIn: 'root' },
  withState<PreferencesState>(() => ({
    theme: toPreference(inject(ThemeService).mode()),
    ...readLocal(),
    saving: false
  })),
  withMethods(
    (
      store,
      api = inject(ProfileApi),
      auth = inject(AuthStore),
      theme = inject(ThemeService),
      document = inject(DOCUMENT)
    ) => {
      /** Makes the settings visible: theme attribute and reduced-motion attribute (SoundService reads soundEnabled itself). */
      const apply = () => {
        theme.setMode(toMode(store.theme()));
        if (store.animationsEnabled()) {
          document.documentElement.removeAttribute(MOTION_ATTRIBUTE);
        } else {
          document.documentElement.setAttribute(MOTION_ATTRIBUTE, 'reduce');
        }
        try {
          localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify({ soundEnabled: store.soundEnabled(), animationsEnabled: store.animationsEnabled() })
          );
        } catch {
          // Storage blocked: the settings still apply for this visit
        }
      };

      const request = (): UpdatePreferencesRequest => ({
        theme: store.theme(),
        soundEnabled: store.soundEnabled(),
        animationsEnabled: store.animationsEnabled()
      });

      /** Applies at once; saved on the account when logged in. Resolves false if saving failed. */
      const update = async (changes: Partial<UpdatePreferencesRequest>): Promise<boolean> => {
        patchState(store, changes);
        apply();
        if (!auth.isLoggedIn()) {
          return true;
        }
        patchState(store, { saving: true });
        try {
          await firstValueFrom(api.updatePreferences(request()));
          return true;
        } catch {
          return false; // the error interceptor already showed why; the setting still applies here
        } finally {
          patchState(store, { saving: false });
        }
      };

      return {
        apply,
        update,

        /** After login: the account's settings win over this browser's. */
        async load(): Promise<void> {
          const saved = await firstValueFrom(api.preferences());
          patchState(store, { theme: saved.theme, soundEnabled: saved.soundEnabled, animationsEnabled: saved.animationsEnabled });
          apply();
        },

        /** The sun / moon button: the other fixed theme. */
        toggleTheme: (): Promise<boolean> => update({ theme: theme.theme() === 'dark' ? 'Light' : 'Dark' })
      };
    }
  ),
  withHooks({
    onInit(store) {
      store.apply();
    }
  })
);
