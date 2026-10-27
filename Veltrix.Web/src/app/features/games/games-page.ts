import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { AuthStore } from '../../core/auth/auth.store';
import { GameDto, GameQuery, GameStatus } from '../../core/models/api.models';
import { Button, Card, EmptyState, Icons, PageHeader, Skeleton } from '../../shared/ui';
import { GameCard } from './game-card';

const SEARCH_DELAY_MS = 300;

/** Games catalog (spec 4.7): search and filters by type and status. Public; staff also see Disabled games. */
@Component({
  selector: 'vx-games-page',
  imports: [FormsModule, LucideAngularModule, Button, Card, EmptyState, PageHeader, Skeleton, GameCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header title="Games" description="Every game runs its own rounds on the server. Virtual VLX points only." />

    <div class="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
      <div class="vx-input-wrap min-w-0 flex-1 sm:max-w-sm">
        <lucide-icon [img]="icons.search" [size]="16" class="vx-input-icon" aria-hidden="true" />
        <input
          class="vx-input"
          type="search"
          placeholder="Search games"
          aria-label="Search games"
          [ngModel]="searchText()"
          (ngModelChange)="searchText.set($event)"
        />
      </div>
      <div class="grid grid-cols-2 gap-3 sm:flex">
        <select class="vx-input sm:w-36" aria-label="Filter by type" [ngModel]="type()" (ngModelChange)="type.set($event)">
          <option [ngValue]="null">All types</option>
          <option ngValue="Crash">Crash</option>
        </select>
        <select class="vx-input sm:w-44" aria-label="Filter by status" [ngModel]="status()" (ngModelChange)="status.set($event)">
          <option [ngValue]="null">All statuses</option>
          @for (s of statuses(); track s) {
            <option [ngValue]="s">{{ s }}</option>
          }
        </select>
      </div>
      @if (searchText() || type() || status()) {
        <button vxButton type="button" variant="ghost" size="sm" (click)="clear()">
          <lucide-icon [img]="icons.close" [size]="14" aria-hidden="true" />Clear
        </button>
      }
    </div>

    @if (games(); as list) {
      @if (list.length === 0) {
        <vx-card>
          <vx-empty-state [icon]="icons.gamepad" title="No games found" message="Try another search or clear the filters." />
        </vx-card>
      } @else {
        <div class="grid gap-4 transition-opacity duration-200 sm:grid-cols-2 md:gap-6 xl:grid-cols-3" [class.opacity-60]="loading()">
          @for (game of list; track game.id) {
            <vx-game-card [game]="game" />
          }
        </div>
      }
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 md:gap-6 xl:grid-cols-3">
        @for (i of [1, 2, 3]; track i) {
          <vx-card><vx-skeleton height="10rem" shape="rect" /></vx-card>
        }
      </div>
    }
  `
})
export class GamesPage {
  private readonly api = inject(GamesApi);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthStore);

  protected readonly icons = Icons;

  // Filters live in the URL (?search=&type=&status=), so a filtered catalog can be shared
  private readonly params = inject(ActivatedRoute).snapshot.queryParamMap;
  protected readonly searchText = signal(this.params.get('search') ?? '');
  private readonly search = signal(this.searchText());
  protected readonly type = signal<'Crash' | null>(this.params.get('type') === 'Crash' ? 'Crash' : null);
  protected readonly status = signal<GameStatus | null>(asStatus(this.params.get('status')));

  protected readonly games = signal<GameDto[] | null>(null);
  protected readonly loading = signal(false);

  /** Disabled games exist only for staff (the server hides them from everyone else). */
  protected readonly statuses = () =>
    (this.auth.isStaff() ? ['Active', 'Maintenance', 'Disabled'] : ['Active', 'Maintenance']) as GameStatus[];

  constructor() {
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => {
      const text = this.searchText().trim();
      clearTimeout(timer);
      timer = setTimeout(() => this.search.set(text), SEARCH_DELAY_MS);
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(timer));

    effect(() => {
      const query: GameQuery = { search: this.search() || null, type: this.type(), status: this.status() };
      untracked(() => {
        void this.router.navigate([], { queryParams: query, replaceUrl: true });
        void this.load(query);
      });
    });
  }

  protected clear(): void {
    this.searchText.set('');
    this.search.set('');
    this.type.set(null);
    this.status.set(null);
  }

  private async load(query: GameQuery): Promise<void> {
    this.loading.set(true);
    try {
      this.games.set(await firstValueFrom(this.api.list(query)));
    } catch {
      this.games.update((current) => current ?? []);
    } finally {
      this.loading.set(false);
    }
  }
}

function asStatus(value: string | null): GameStatus | null {
  return value === 'Active' || value === 'Maintenance' || value === 'Disabled' ? value : null;
}
