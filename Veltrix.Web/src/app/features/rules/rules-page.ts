import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { PagesApi } from '../../core/api/pages.api';
import { AuthStore } from '../../core/auth/auth.store';
import { I18n } from '../../core/i18n/i18n';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { StaticPageDto } from '../../core/models/api.models';
import { Button, Card, Icons, Markdown, PageHeader, Skeleton } from '../../shared/ui';

/**
 * Rules (guest rights in spec section 3) and "why Crash has no winning strategy" (spec 4.8).
 * The English text comes from the StaticPages table, so it can be changed without a new deployment;
 * the Georgian text is in the one Georgian file (src/i18n/ka.json, section "pages").
 * A player who keeps the end of the rules on screen for READ_TIME_MS tells the server once
 * (POST /api/pages/no-winning-strategy/read); the server decides whether that completes "Know the rules".
 * The time matters: on a wide screen the whole page fits, so merely opening it must not count as reading.
 */
/** How long the end of the rules must stay visible before it counts as read. */
export const READ_TIME_MS = 10_000;

@Component({
  selector: 'vx-rules-page',
  imports: [RouterLink, LucideAngularModule, Button, Card, Markdown, PageHeader, Skeleton, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header [title]="'Rules' | t" [description]="'How a round works, how fairness is proven, and why no strategy beats the game.' | t" />

    <div class="grid gap-4 md:gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
      <div class="flex min-w-0 flex-col gap-4 md:gap-6">
        @for (page of shown(); track page.slug) {
          <vx-card [title]="page.title">
            <vx-markdown [source]="page.content" />
          </vx-card>
        } @empty {
          @for (i of [1, 2]; track i) {
            <vx-card>
              <div class="flex flex-col gap-3">
                <vx-skeleton width="40%" height="1rem" />
                <vx-skeleton height="0.875rem" />
                <vx-skeleton height="0.875rem" />
                <vx-skeleton width="70%" height="0.875rem" />
              </div>
            </vx-card>
          }
        }
        @if (shown().length > 0) {
          <!-- The end of the rules: reaching it counts as "read" -->
          <div #end class="h-px" aria-hidden="true"></div>
        }
      </div>

      <div class="flex flex-col gap-4 md:gap-6">
        <vx-card [title]="'Check any round' | t" [subtitle]="'The crash point is fixed and hashed before betting opens.' | t">
          <a vxButton routerLink="/fairness" variant="secondary" block>
            <lucide-icon [img]="icons.shield" [size]="16" aria-hidden="true" />{{ 'Provably fair' | t }}
          </a>
        </vx-card>
        <vx-card [title]="'Play responsibly' | t" [subtitle]="'A daily limit, break reminders and a pause you cannot undo.' | t">
          <a vxButton routerLink="/limits" variant="secondary" block>
            <lucide-icon [img]="icons.responsible" [size]="16" aria-hidden="true" />{{ 'Responsible play' | t }}
          </a>
        </vx-card>
        <vx-card>
          <p class="flex items-start gap-2 text-[13px] text-muted">
            <lucide-icon [img]="icons.vlx" [size]="16" class="mt-0.5 shrink-0 text-accent" aria-hidden="true" />
            {{ 'Educational project. VLX points are virtual: they cannot be bought, sold, withdrawn or exchanged.' | t }}
          </p>
        </vx-card>
      </div>
    </div>
  `
})
export class RulesPage {
  private readonly api = inject(PagesApi);
  private readonly auth = inject(AuthStore);
  private readonly i18n = inject(I18n);
  private readonly end = viewChild<ElementRef<HTMLElement>>('end');

  protected readonly icons = Icons;
  private readonly pages = signal<StaticPageDto[]>([]);

  /** In Georgian, each page's Georgian version from ka.json (the English one from the database otherwise). */
  protected readonly shown = computed(() =>
    this.pages().map((page) => {
      const translated = this.i18n.page(page.slug);
      return translated ? { ...page, ...translated } : page;
    })
  );

  constructor() {
    void Promise.all(['rules', 'no-winning-strategy'].map((slug) => firstValueFrom(this.api.get(slug)).catch(() => null))).then(
      (pages) => this.pages.set(pages.filter((p): p is StaticPageDto => p !== null))
    );

    // Only players collect achievements; staff and guests send nothing
    effect((onCleanup) => {
      const end = this.end()?.nativeElement;
      if (!end || this.auth.role() !== 'Player' || typeof IntersectionObserver === 'undefined') {
        return;
      }
      let timer: ReturnType<typeof setTimeout> | undefined;
      const observer = new IntersectionObserver((entries) => {
        clearTimeout(timer); // scrolled away before the time was up: start again next time
        if (entries.some((e) => e.isIntersecting)) {
          timer = setTimeout(() => {
            observer.disconnect();
            void firstValueFrom(this.api.markRead('no-winning-strategy')).catch(() => undefined);
          }, READ_TIME_MS);
        }
      });
      observer.observe(end);
      onCleanup(() => {
        clearTimeout(timer);
        observer.disconnect();
      });
    });
  }
}
