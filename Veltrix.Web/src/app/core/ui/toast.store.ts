import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
}

const MAX_VISIBLE = 4;

/** Short pop-up messages (success / error / info). Errors stay a little longer. */
export const ToastStore = signalStore(
  { providedIn: 'root' },
  withState({ toasts: [] as Toast[] }),
  withMethods((store) => {
    let nextId = 1;

    const dismiss = (id: number) =>
      patchState(store, (state) => ({ toasts: state.toasts.filter((t) => t.id !== id) }));

    const show = (kind: ToastKind, message: string, durationMs: number) => {
      const id = nextId++;
      patchState(store, (state) => ({
        toasts: [...state.toasts.slice(-(MAX_VISIBLE - 1)), { id, kind, message }]
      }));
      setTimeout(() => dismiss(id), durationMs);
    };

    return {
      success: (message: string) => show('success', message, 3500),
      info: (message: string) => show('info', message, 3500),
      error: (message: string) => show('error', message, 6000),
      dismiss
    };
  })
);
