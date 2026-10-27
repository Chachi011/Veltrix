import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { PagesApi } from '../../core/api/pages.api';
import { StaticPageDto } from '../../core/models/api.models';
import { Button, Card, Icons, Markdown, PageHeader, Skeleton } from '../../shared/ui';

/**
 * Rules (guest rights in spec section 3) and "why Crash has no winning strategy" (spec 4.8).
 * The text comes from the StaticPages table, so it can be changed without a new deployment.
 */
@Component({
  selector: 'vx-rules-page',
  imports: [RouterLink, LucideAngularModule, Button, Card, Markdown, PageHeader, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header title="Rules" description="How a round works, how fairness is proven, and why no strategy beats the game." />

    <div class="grid gap-4 md:gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
      <div class="flex min-w-0 flex-col gap-4 md:gap-6">
        @for (page of pages(); track page.slug) {
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
      </div>

      <div class="flex flex-col gap-4 md:gap-6">
        <vx-card title="Check any round" subtitle="The crash point is fixed and hashed before betting opens.">
          <a vxButton routerLink="/fairness" variant="secondary" block>
            <lucide-icon [img]="icons.shield" [size]="16" aria-hidden="true" />Provably fair
          </a>
        </vx-card>
        <vx-card title="Play responsibly" subtitle="A daily limit, break reminders and a pause you cannot undo.">
          <a vxButton routerLink="/limits" variant="secondary" block>
            <lucide-icon [img]="icons.responsible" [size]="16" aria-hidden="true" />Responsible play
          </a>
        </vx-card>
        <vx-card>
          <p class="flex items-start gap-2 text-[13px] text-muted">
            <lucide-icon [img]="icons.vlx" [size]="16" class="mt-0.5 shrink-0 text-accent" aria-hidden="true" />
            Educational project. VLX points are virtual: they cannot be bought, sold, withdrawn or exchanged.
          </p>
        </vx-card>
      </div>
    </div>
  `
})
export class RulesPage {
  private readonly api = inject(PagesApi);

  protected readonly icons = Icons;
  protected readonly pages = signal<StaticPageDto[]>([]);

  constructor() {
    void Promise.all(['rules', 'no-winning-strategy'].map((slug) => firstValueFrom(this.api.get(slug)).catch(() => null))).then(
      (pages) => this.pages.set(pages.filter((p): p is StaticPageDto => p !== null))
    );
  }
}
