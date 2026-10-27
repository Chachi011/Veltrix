import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { StatsApi } from '../../core/api/stats.api';
import { AuthStore } from '../../core/auth/auth.store';
import { GameDto, LeaderboardEntryDto } from '../../core/models/api.models';
import { Avatar, Button, Card, EmptyState, Icons, MultiplierPill, PageHeader, Skeleton } from '../../shared/ui';
import { GameCard } from '../games/game-card';
import { LiveRoundCard } from './live-round-card';

/**
 * Home (spec 4.7, guest rights): available games, the live round, a short leaderboard.
 * Everything here is public, so a guest sees the platform working before registering.
 */
@Component({
  selector: 'vx-home-page',
  imports: [RouterLink, LucideAngularModule, Avatar, Button, Card, EmptyState, MultiplierPill, PageHeader, Skeleton, GameCard, LiveRoundCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home-page.html'
})
export class HomePage {
  private readonly gamesApi = inject(GamesApi);
  private readonly statsApi = inject(StatsApi);
  protected readonly auth = inject(AuthStore);

  protected readonly icons = Icons;
  protected readonly games = signal<GameDto[] | null>(null);
  protected readonly leaders = signal<LeaderboardEntryDto[] | null>(null);

  constructor() {
    void firstValueFrom(this.gamesApi.list())
      .then((games) => this.games.set(games.filter((g) => g.status !== 'Disabled').slice(0, 3)))
      .catch(() => this.games.set([]));
    void firstValueFrom(this.statsApi.leaderboard('Day', 'Multiplier', 5))
      .then((board) => this.leaders.set(board.entries))
      .catch(() => this.leaders.set([]));
  }
}
