import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AssistantApi } from '../../core/api/assistant.api';
import { problemMessage, toProblem } from '../../core/http/problem';
import { AssistantMessageDto } from '../../core/models/api.models';

/** Same limit as the server (AssistantMessage.MaxQuestionLength). */
export const ASSISTANT_MAX_LENGTH = 500;

type AssistantState = {
  /** Whose conversation is loaded: a different player on the same tab starts from nothing. */
  userId: number | null;
  messages: AssistantMessageDto[];
  loaded: boolean;
  loading: boolean;
  sending: boolean;
  /** The question being answered right now: shown at once, removed again if the AI cannot answer. */
  pending: string | null;
  /** What is typed in the box. Kept here, so closing and reopening the drawer does not lose it. */
  draft: string;
  /** False when the server has no AI set up: the drawer says so and the box is disabled. Null = not known yet. */
  configured: boolean | null;
  /** Why the last question got no answer (the server's message, in English), shown in the drawer. */
  error: string | null;
};

const initialState: AssistantState = {
  userId: null,
  messages: [],
  loaded: false,
  loading: false,
  sending: false,
  pending: null,
  draft: '',
  configured: null,
  error: null
};

/**
 * The AI assistant chat. The conversation is kept in two places:
 * - the DATABASE (the server saves every question and answer), so it is still there after a reload or on another device;
 * - this STORE for the rest of the browser session, so reopening the drawer shows it at once, without a request.
 *
 * Lives in the lazy assistant chunk (only players who open the drawer download it), but is a root singleton,
 * so it survives closing the drawer.
 */
export const AssistantStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ messages, pending, draft, sending, configured }) => ({
    isEmpty: computed(() => messages().length === 0 && pending() === null),
    canSend: computed(() => {
      const text = draft().trim();
      return configured() !== false && !sending() && text.length > 0 && text.length <= ASSISTANT_MAX_LENGTH;
    })
  })),
  withMethods((store, api = inject(AssistantApi)) => ({
    /** Called when the drawer opens: loads whether the AI is set up and the saved conversation, once per player and session. */
    async open(userId: number): Promise<void> {
      if (store.userId() !== userId) {
        patchState(store, { ...initialState, userId });
      }
      if (store.loaded() || store.loading()) {
        return;
      }
      patchState(store, { loading: true });
      try {
        const [status, messages] = await Promise.all([firstValueFrom(api.status()), firstValueFrom(api.history())]);
        patchState(store, { configured: status.configured, messages, loaded: true });
      } catch {
        // The error toast already explains it; the next opening tries again
      } finally {
        patchState(store, { loading: false });
      }
    },

    setDraft(text: string): void {
      patchState(store, { draft: text });
    },

    /**
     * Sends the typed question (or a suggested one). Resolves true when the answer arrived.
     * If the AI cannot answer, the question goes back into the box, so nothing typed is lost, and `error` says why
     * (shown in the drawer itself: a toast could be missed while the drawer is open).
     */
    async ask(suggestion?: string): Promise<boolean> {
      const fromBox = suggestion === undefined;
      const question = (fromBox ? store.draft() : suggestion).trim();
      if (!question || question.length > ASSISTANT_MAX_LENGTH || store.sending() || store.configured() === false) {
        return false;
      }

      patchState(store, { sending: true, pending: question, error: null, draft: fromBox ? '' : store.draft() });
      try {
        const reply = await firstValueFrom(api.ask(question));
        patchState(store, (s) => ({ messages: [...s.messages, reply.question, reply.answer] }));
        return true;
      } catch (error) {
        patchState(store, { error: problemMessage(toProblem(error)) });
        if (fromBox && store.draft() === '') {
          patchState(store, { draft: question });
        }
        return false;
      } finally {
        patchState(store, { sending: false, pending: null });
      }
    },

    /** "New chat": deletes the conversation on the server too. */
    async clear(): Promise<boolean> {
      try {
        await firstValueFrom(api.clear());
        patchState(store, { messages: [] });
        return true;
      } catch {
        return false;
      }
    }
  }))
);
