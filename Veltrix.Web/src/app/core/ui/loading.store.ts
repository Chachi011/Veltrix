import { computed } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';

/** Counts HTTP requests in flight; the shell shows a thin progress bar while it is above zero. */
export const LoadingStore = signalStore(
  { providedIn: 'root' },
  withState({ pending: 0 }),
  withComputed((store) => ({
    active: computed(() => store.pending() > 0)
  })),
  withMethods((store) => ({
    start: () => patchState(store, (s) => ({ pending: s.pending + 1 })),
    stop: () => patchState(store, (s) => ({ pending: Math.max(0, s.pending - 1) }))
  }))
);
