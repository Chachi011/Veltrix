import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, Injector, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AssistantApi } from '../../core/api/assistant.api';
import { AuthStore } from '../../core/auth/auth.store';
import { openAssistantDrawer } from './assistant-drawer';
import { AssistantStore } from './assistant.store';

/** Lets the drawer load (status + history) and render. */
async function settle(): Promise<void> {
  for (let i = 0; i < 3; i++) {
    await new Promise((resolve) => setTimeout(resolve));
    await TestBed.inject(ApplicationRef).whenStable();
  }
}

const panel = () => document.querySelector<HTMLElement>('.vx-drawer-panel')!;

describe('AssistantDrawer', () => {
  let api: jasmine.SpyObj<AssistantApi>;

  beforeEach(() => {
    api = jasmine.createSpyObj<AssistantApi>('AssistantApi', ['status', 'history', 'ask', 'clear']);
    api.history.and.returnValue(of([]));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: AssistantApi, useValue: api },
        { provide: AuthStore, useValue: { user: signal({ id: 7, nickname: 'player1' }) } }
      ]
    });
  });

  afterEach(async () => {
    panel()?.querySelector<HTMLButtonElement>('button[aria-label="Close"]')?.click();
    await settle();
  });

  it('says in the panel that the assistant is not configured, and disables the box and the suggestions', async () => {
    api.status.and.returnValue(of({ configured: false }));

    openAssistantDrawer(TestBed.inject(Injector), () => {});
    await settle();

    const notice = panel().querySelector('[role=status]');
    expect(notice?.textContent).toContain('The assistant is not configured.');
    expect(panel().querySelector('textarea')!.disabled).toBeTrue();
    const suggestions = [...panel().querySelectorAll<HTMLButtonElement>('button')].filter((b) => b.textContent?.includes('How does Crash work?'));
    expect(suggestions.length).toBe(1);
    expect(suggestions[0].disabled).toBeTrue();
  });

  it('shows why a question got no answer inside the panel, not only in a toast', async () => {
    const detail = 'The assistant is not available right now. Please try again in a moment.';
    api.status.and.returnValue(of({ configured: true }));
    api.ask.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503, error: { status: 503, detail } })));

    openAssistantDrawer(TestBed.inject(Injector), () => {});
    await settle();
    TestBed.inject(AssistantStore).setDraft('Anyone there?');
    await settle();
    panel().querySelector<HTMLButtonElement>('button[aria-label="Send"]')!.click();
    await settle();

    expect(api.ask).toHaveBeenCalledWith('Anyone there?');
    expect(panel().querySelector('[role=alert]')?.textContent).toContain(detail);
    expect(panel().querySelector('textarea')!.value).toBe('Anyone there?'); // put back, nothing lost
  });
});
