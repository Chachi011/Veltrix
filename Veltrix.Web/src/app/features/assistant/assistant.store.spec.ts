import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AssistantApi } from '../../core/api/assistant.api';
import { AssistantMessageDto } from '../../core/models/api.models';
import { ASSISTANT_MAX_LENGTH, AssistantStore } from './assistant.store';

const message = (id: number, role: AssistantMessageDto['role'], text: string): AssistantMessageDto => ({
  id,
  role,
  text,
  createdAt: '2026-10-30T18:00:00Z'
});

describe('AssistantStore', () => {
  let store: InstanceType<typeof AssistantStore>;
  let api: jasmine.SpyObj<AssistantApi>;

  beforeEach(() => {
    api = jasmine.createSpyObj<AssistantApi>('AssistantApi', ['history', 'ask', 'clear']);
    api.history.and.returnValue(of([message(1, 'User', 'Hi'), message(2, 'Assistant', 'Hello!')]));
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: AssistantApi, useValue: api }]
    });
    store = TestBed.inject(AssistantStore);
  });

  it('loads the saved conversation once per session', async () => {
    await store.open(7);
    await store.open(7); // reopening the drawer: no second request

    expect(api.history).toHaveBeenCalledTimes(1);
    expect(store.messages().map((m) => m.text)).toEqual(['Hi', 'Hello!']);
    expect(store.isEmpty()).toBeFalse();
  });

  it('another player on the same tab starts from their own conversation', async () => {
    await store.open(7);
    store.setDraft('half-typed');
    api.history.and.returnValue(of([]));

    await store.open(8);

    expect(api.history).toHaveBeenCalledTimes(2);
    expect(store.messages()).toEqual([]);
    expect(store.draft()).toBe('');
  });

  it('asking adds the saved question and the answer, and empties the box', async () => {
    await store.open(7);
    store.setDraft('  How does Crash work?  ');
    api.ask.and.returnValue(
      of({ question: message(3, 'User', 'How does Crash work?'), answer: message(4, 'Assistant', 'Cash out before it crashes.') })
    );

    const answered = await store.ask();

    expect(answered).toBeTrue();
    expect(api.ask).toHaveBeenCalledWith('How does Crash work?'); // trimmed
    expect(store.messages().map((m) => m.id)).toEqual([1, 2, 3, 4]);
    expect(store.draft()).toBe('');
    expect(store.sending()).toBeFalse();
    expect(store.pending()).toBeNull();
  });

  it('a suggested question is sent without touching what is typed', async () => {
    await store.open(7);
    store.setDraft('my own question');
    api.ask.and.returnValue(of({ question: message(3, 'User', 'Q'), answer: message(4, 'Assistant', 'A') }));

    await store.ask('How do I set a daily limit?');

    expect(api.ask).toHaveBeenCalledWith('How do I set a daily limit?');
    expect(store.draft()).toBe('my own question');
  });

  it('when the AI cannot answer, the question goes back into the box', async () => {
    await store.open(7);
    store.setDraft('Anyone there?');
    api.ask.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503 })));

    const answered = await store.ask();

    expect(answered).toBeFalse();
    expect(store.draft()).toBe('Anyone there?');
    expect(store.messages().length).toBe(2); // nothing added
    expect(store.pending()).toBeNull();
  });

  it('cannot send an empty or too long question', async () => {
    await store.open(7);

    store.setDraft('   ');
    expect(store.canSend()).toBeFalse();
    expect(await store.ask()).toBeFalse();

    store.setDraft('x'.repeat(ASSISTANT_MAX_LENGTH + 1));
    expect(store.canSend()).toBeFalse();
    expect(await store.ask()).toBeFalse();

    expect(api.ask).not.toHaveBeenCalled();
  });

  it('"New chat" empties the conversation', async () => {
    await store.open(7);
    api.clear.and.returnValue(of(undefined));

    expect(await store.clear()).toBeTrue();
    expect(store.messages()).toEqual([]);
    expect(store.isEmpty()).toBeTrue();
  });
});
