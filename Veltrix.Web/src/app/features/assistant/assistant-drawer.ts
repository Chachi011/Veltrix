import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  untracked,
  viewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth.store';
import { I18n, mark } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { reducedMotion } from '../../shared/motion';
import { Button, DrawerShell, Icons, Markdown, ModalService, Skeleton } from '../../shared/ui';
import { ASSISTANT_MAX_LENGTH, AssistantStore } from './assistant.store';

/**
 * Questions a new player usually has. None of them is about winning: the assistant explains the game and its
 * safety tools, it never gives betting tips (see AssistantPrompts on the server).
 */
const SUGGESTIONS = [
  mark('How does Crash work?'),
  mark('How can I check that a round was fair?'),
  mark('What are VLX and how do I get them?'),
  mark('How do I set a daily limit?')
];

/**
 * The AI assistant chat, in a drawer on the right (full width on phones). Players only (the top-bar button and
 * the API both check it). The answers come from the server, which asks an n8n workflow.
 */
@Component({
  selector: 'vx-assistant-drawer',
  imports: [FormsModule, LucideAngularModule, DrawerShell, Markdown, Button, Skeleton, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-drawer [title]="'AI assistant' | t" [description]="'Questions about the game, VLX and your safety tools' | t">
      @if (store.loading() && !store.loaded()) {
        <div class="flex flex-col gap-3" aria-hidden="true">
          <vx-skeleton width="70%" height="3rem" shape="rect" class="self-end" />
          <vx-skeleton width="85%" height="5rem" shape="rect" />
        </div>
      } @else if (store.isEmpty()) {
        <!-- First visit: what the assistant is for, and what it will not do -->
        <div class="flex flex-col items-center gap-4 py-4 text-center">
          <span class="grid size-12 place-items-center rounded-full bg-primary/12 text-primary-text" aria-hidden="true">
            <lucide-icon [img]="icons.sparkles" [size]="22" />
          </span>
          <div>
            <p class="text-base font-semibold">{{ 'Hi, {name}!' | t: { name: nickname() } }}</p>
            <p class="mt-1 text-sm text-muted">
              {{ 'I can explain the rules of Crash, provably fair rounds, VLX and the responsible play tools.' | t }}
            </p>
          </div>
          <p class="flex items-start gap-2 rounded-control border border-border bg-surface-2 p-3 text-left text-[13px] text-muted">
            <lucide-icon [img]="icons.info" [size]="15" class="mt-0.5 shrink-0" aria-hidden="true" />
            {{ 'I do not give betting tips: every round is random, and no strategy beats the house edge.' | t }}
          </p>
          <div class="flex w-full flex-col gap-2">
            @for (question of suggestions; track question) {
              <button
                vxButton
                type="button"
                variant="outline"
                size="sm"
                class="h-auto! justify-start py-2 text-left whitespace-normal"
                [disabled]="store.sending()"
                (click)="send(question)"
              >
                {{ question | t }}
              </button>
            }
          </div>
        </div>
      } @else {
        <ol class="flex flex-col gap-3" role="log" aria-live="polite" [attr.aria-label]="'Conversation' | t">
          @for (message of store.messages(); track message.id) {
            @if (message.role === 'User') {
              <li class="ml-10 max-w-[85%] self-end rounded-card rounded-br-md bg-primary px-3.5 py-2.5 text-sm leading-6 break-words whitespace-pre-wrap text-on-primary">
                <span class="sr-only">{{ 'You' | t }}:</span> {{ message.text }}
              </li>
            } @else {
              <li class="mr-6 flex gap-2.5">
                <span class="mt-1 grid size-7 shrink-0 place-items-center rounded-full bg-primary/12 text-primary-text" aria-hidden="true">
                  <lucide-icon [img]="icons.sparkles" [size]="14" />
                </span>
                <div class="min-w-0 rounded-card rounded-tl-md border border-border bg-surface-2 px-3.5 py-2.5">
                  <span class="sr-only">{{ 'Assistant' | t }}:</span>
                  <vx-markdown [source]="message.text" class="gap-2! break-words [&_li]:text-text [&_p]:text-text" />
                </div>
              </li>
            }
          }
          @if (store.pending(); as pending) {
            <li class="ml-10 max-w-[85%] self-end rounded-card rounded-br-md bg-primary/80 px-3.5 py-2.5 text-sm leading-6 break-words whitespace-pre-wrap text-on-primary">
              <span class="sr-only">{{ 'You' | t }}:</span> {{ pending }}
            </li>
            <li class="mr-6 flex items-center gap-2.5 text-sm text-muted">
              <span class="grid size-7 shrink-0 place-items-center rounded-full bg-primary/12 text-primary-text" aria-hidden="true">
                <lucide-icon [img]="icons.loader" [size]="14" class="animate-spin" />
              </span>
              {{ 'Thinking…' | t }}
            </li>
          }
        </ol>
      }
      <div #end aria-hidden="true"></div>

      <div drawer-footer class="flex flex-col gap-2">
        <div class="flex items-end gap-2">
          <label for="assistant-question" class="sr-only">{{ 'Your question' | t }}</label>
          <textarea
            #box
            id="assistant-question"
            rows="2"
            class="vx-input h-auto! max-h-36 min-h-11 flex-1 resize-none py-2.5"
            [ngModel]="store.draft()"
            (ngModelChange)="store.setDraft($event)"
            [attr.maxlength]="maxLength"
            [placeholder]="'Ask a question about VELTRIX' | t"
            (keydown.enter)="onEnter($event)"
          ></textarea>
          <button
            vxButton
            type="button"
            variant="primary"
            size="icon"
            [disabled]="!store.canSend()"
            [attr.aria-label]="'Send' | t"
            (click)="send()"
          >
            <lucide-icon [img]="icons.send" [size]="18" aria-hidden="true" />
          </button>
        </div>
        <div class="flex min-h-8 items-center justify-between gap-3 text-xs text-muted">
          <span>
            {{ 'Answers are written by AI and can be wrong.' | t }}
            @if (nearLimit()) {
              <span class="num ml-1" aria-live="polite">{{ store.draft().length }}/{{ maxLength }}</span>
            }
          </span>
          @if (store.messages().length > 0) {
            <button vxButton type="button" variant="ghost" size="sm" class="shrink-0" [disabled]="store.sending()" (click)="startOver()">
              <lucide-icon [img]="icons.trash" [size]="14" aria-hidden="true" />
              {{ 'New chat' | t }}
            </button>
          }
        </div>
      </div>
    </vx-drawer>
  `
})
export class AssistantDrawer {
  protected readonly store = inject(AssistantStore);
  private readonly auth = inject(AuthStore);
  private readonly i18n = inject(I18n);
  private readonly modal = inject(ModalService);
  private readonly injector = inject(Injector);

  protected readonly icons = Icons;
  protected readonly suggestions = SUGGESTIONS;
  protected readonly maxLength = ASSISTANT_MAX_LENGTH;

  protected readonly nickname = computed(() => this.auth.user()?.nickname ?? '');
  /** The character counter appears only when it matters. */
  protected readonly nearLimit = computed(() => this.store.draft().length > ASSISTANT_MAX_LENGTH - 100);

  private readonly box = viewChild<ElementRef<HTMLTextAreaElement>>('box');
  private readonly end = viewChild<ElementRef<HTMLElement>>('end');

  constructor() {
    const userId = this.auth.user()?.id;
    if (userId !== undefined) {
      void this.store.open(userId);
    }

    // A new message (or the thinking indicator) scrolls the conversation to the bottom
    effect(() => {
      this.store.messages();
      this.store.pending();
      untracked(() =>
        afterNextRender(
          () => this.end()?.nativeElement.scrollIntoView({ block: 'end', behavior: reducedMotion() ? 'auto' : 'smooth' }),
          { injector: this.injector }
        )
      );
    });

    // Desktop: ready to type at once. Phones: no focus, so the keyboard does not cover the suggestions.
    afterNextRender(() => {
      if (window.matchMedia?.('(pointer: fine)').matches) {
        this.box()?.nativeElement.focus();
      }
    });
  }

  /** Enter sends; Shift+Enter makes a new line (and Enter while an input method is composing does nothing). */
  protected onEnter(event: Event): void {
    const key = event as KeyboardEvent;
    if (key.shiftKey || key.isComposing) {
      return;
    }
    event.preventDefault();
    this.send();
  }

  /** The typed question, or a suggestion (sent in the player's language). */
  protected send(suggestion?: string): void {
    void this.store.ask(suggestion === undefined ? undefined : this.i18n.t(suggestion));
  }

  protected async startOver(): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: this.i18n.t('Start a new chat?'),
      message: this.i18n.t('This deletes the whole conversation with the assistant.'),
      confirmText: this.i18n.t('Delete chat'),
      tone: 'danger'
    });
    if (confirmed) {
      await this.store.clear();
      this.box()?.nativeElement.focus();
    }
  }
}

let openDrawer: { close(): void } | null = null;

/**
 * Opens the assistant drawer (or does nothing if it is already open). Called by the top bar after it loaded this
 * file on demand, so the drawer, its store and the CDK Dialog are not in the first download.
 * `onClosed` runs when the drawer closes (Escape, the close button, the backdrop or a navigation).
 */
export function openAssistantDrawer(injector: Injector, onClosed: () => void): void {
  if (openDrawer) {
    return;
  }
  const modal = injector.get(ModalService);
  const i18n = injector.get(I18n);
  const ref = modal.openDrawer<void, void, AssistantDrawer>(AssistantDrawer, {
    ariaLabel: i18n.t('AI assistant'),
    width: '440px'
  });
  openDrawer = ref;
  ref.closed.subscribe(() => {
    openDrawer = null;
    onClosed();
  });
}
