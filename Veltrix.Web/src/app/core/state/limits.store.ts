import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { LimitsApi } from '../api/limits.api';
import { PlayerLimitsDto, SelfExclusionPeriod, UpdateLimitsRequest } from '../models/api.models';

type LimitsState = {
  limits: PlayerLimitsDto | null;
  saving: boolean;
  excluding: boolean;
};

const initialState: LimitsState = { limits: null, saving: false, excluding: false };

/** Default when the limits are not loaded yet (same as the server's default). */
export const DEFAULT_REMINDER_MIN = 60;

/**
 * The player's responsible play settings. App-wide, because the Crash page (betting paused) and the
 * session reminder need them too, not only the Responsible play page.
 * The rules themselves are enforced by the server; this store only shows them.
 */
export const LimitsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ limits }) => ({
    isSelfExcluded: computed(() => limits()?.isSelfExcluded ?? false),
    selfExcludedUntil: computed(() => limits()?.selfExcludedUntil ?? null),
    reminderMinutes: computed(() => limits()?.sessionReminderMin ?? DEFAULT_REMINDER_MIN)
  })),
  withMethods((store, api = inject(LimitsApi)) => ({
    async load(): Promise<void> {
      patchState(store, { limits: await firstValueFrom(api.get()) });
    },

    /** Resolves true on success; the error interceptor shows the reason otherwise. */
    async update(request: UpdateLimitsRequest): Promise<boolean> {
      patchState(store, { saving: true });
      try {
        patchState(store, { limits: await firstValueFrom(api.update(request)) });
        return true;
      } catch {
        return false;
      } finally {
        patchState(store, { saving: false });
      }
    },

    async selfExclude(period: SelfExclusionPeriod): Promise<boolean> {
      patchState(store, { excluding: true });
      try {
        patchState(store, { limits: await firstValueFrom(api.selfExclude(period)) });
        return true;
      } catch {
        return false;
      } finally {
        patchState(store, { excluding: false });
      }
    },

    reset(): void {
      patchState(store, initialState);
    }
  }))
);
