import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal, effect, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/profile.api';
import { AuthStore } from '../../core/auth/auth.store';
import { LocalDatePipe, ServerTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { PublicProfileDto } from '../../core/models/api.models';
import { VlxPipe } from '../../shared/pipes';
import {
  Avatar,
  Badge,
  Button,
  Card,
  EmptyState,
  Icons,
  LevelProgress,
  MultiplierPill,
  PageHeader,
  Skeleton,
  StatCard,
  achievementIcon
} from '../../shared/ui';

/**
 * Public player profile (spec 4.6): nickname, avatar, level, achievements and best results. Open to guests.
 * Built from a separate DTO that has no private fields (email, balance, date of birth), so they can never leak.
 */
@Component({
  selector: 'vx-player-page',
  imports: [
    RouterLink,
    LucideAngularModule,
    VlxPipe,
    LocalDatePipe,
    ServerTextPipe,
    TranslatePipe,
    Avatar,
    Badge,
    Button,
    Card,
    EmptyState,
    LevelProgress,
    MultiplierPill,
    PageHeader,
    Skeleton,
    StatCard
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header [title]="profile()?.nickname ?? nickname()" [description]="'Public profile: level and best results.' | t" />

    @if (notFound()) {
      <vx-card>
        <vx-empty-state [icon]="icons.user" [title]="'Player not found' | t" [message]="'This nickname does not exist, or the account is not public.' | t">
          <a vxButton routerLink="/leaderboard" variant="primary" size="sm">{{ 'Open the leaderboard' | t }}</a>
        </vx-empty-state>
      </vx-card>
    } @else if (profile(); as p) {
      <vx-card>
        <div class="flex flex-col gap-5 sm:flex-row sm:items-center">
          <vx-avatar [src]="p.avatarUrl" [name]="p.nickname" size="xl" />
          <div class="min-w-0 flex-1">
            <h2 class="flex flex-wrap items-center gap-2 text-xl font-semibold">
              <span class="truncate">{{ p.nickname }}</span>
              @if (isMe()) {
                <vx-badge tone="primary">{{ 'You' | t }}</vx-badge>
              }
            </h2>
            <p class="mt-1 text-sm text-muted">
              {{ 'Level {level}' | t: { level: p.level } }}{{ p.levelTitle ? ' · ' + (p.levelTitle | serverText) : '' }} ·
              {{ 'member since {date}' | t: { date: (p.memberSince | vxDate: 'mediumDate') } }}
            </p>
          </div>
          @if (isMe()) {
            <a vxButton routerLink="/profile" variant="secondary" size="sm">
              <lucide-icon [img]="icons.edit" [size]="15" aria-hidden="true" />{{ 'Edit my profile' | t }}
            </a>
          }
        </div>
      </vx-card>

      <div class="mt-4 grid gap-4 md:mt-6 md:gap-6 lg:grid-cols-2">
        <vx-card [title]="'Level' | t">
          <vx-level-progress [profile]="p" />
        </vx-card>
        <vx-card [title]="'Achievements' | t">
          @if (p.badges.length === 0) {
            <p class="text-sm text-muted">{{ 'No achievements yet.' | t }}</p>
          } @else {
            <ul class="flex flex-wrap gap-2">
              @for (badge of p.badges; track badge.code) {
                <li
                  class="flex items-center gap-1.5 rounded-full border border-accent/30 bg-accent/10 py-1 pr-3 pl-2 text-xs font-medium text-accent"
                  [title]="'Unlocked {date}' | t: { date: (badge.unlockedAt | vxDate: 'mediumDate') }"
                >
                  <lucide-icon [img]="iconFor(badge.icon)" [size]="14" aria-hidden="true" />{{ badge.title | serverText }}
                </li>
              }
            </ul>
          }
        </vx-card>
      </div>

      <div class="mt-4 grid grid-cols-2 gap-4 md:mt-6 md:gap-6 lg:grid-cols-4">
        <vx-stat-card [label]="'Bets placed' | t" [value]="p.totalBets" [icon]="icons.history" />
        <vx-stat-card
          [label]="'Cashed out' | t"
          [value]="p.cashOutCount"
          [icon]="icons.trendingUp"
          [hint]="p.totalBets ? ('{percent}% of bets' | t: { percent: ((p.cashOutCount * 100) / p.totalBets).toFixed(1) }) : undefined"
        />
        <vx-stat-card [label]="'Best multiplier' | t" [value]="p.biggestMultiplier" [decimals]="2" unit="x" [icon]="icons.zap" />
        <vx-stat-card [label]="'Biggest win' | t" [value]="p.biggestWin" [decimals]="2" unit="VLX" [icon]="icons.vlx" />
      </div>

      <vx-card class="mt-4 md:mt-6" [title]="'Best cash-outs' | t" [subtitle]="'The highest multipliers this player cashed out at.' | t" padding="none">
        @if (p.bestCashOuts.length === 0) {
          <vx-empty-state [icon]="icons.zap" [title]="'No cash-outs yet' | t" [message]="'Best results appear here after the first win.' | t" />
        } @else {
          <div class="vx-table-wrap">
            <table class="vx-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>{{ 'Multiplier' | t }}</th>
                  <th class="text-right">{{ 'Payout' | t }}</th>
                  <th class="hidden text-right sm:table-cell">{{ 'Date' | t }}</th>
                </tr>
              </thead>
              <tbody>
                @for (best of p.bestCashOuts; track $index) {
                  <tr>
                    <td class="num text-muted">{{ $index + 1 }}</td>
                    <td><vx-multiplier-pill [value]="best.multiplier" /></td>
                    <td class="num text-right font-semibold text-success">+{{ best.payout | vlx: false }}</td>
                    <td class="hidden text-right text-muted sm:table-cell">{{ best.cashedOutAt | vxDate: 'MMM d, y' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </vx-card>
    } @else {
      <vx-card>
        <div class="flex items-center gap-5">
          <vx-skeleton width="96px" height="96px" shape="circle" />
          <div class="flex flex-1 flex-col gap-2">
            <vx-skeleton width="40%" height="1.25rem" />
            <vx-skeleton width="60%" height="0.875rem" />
          </div>
        </div>
      </vx-card>
    }
  `
})
export class PlayerPage {
  private readonly api = inject(ProfileApi);
  private readonly auth = inject(AuthStore);

  /** From the route (/players/:nickname), via withComponentInputBinding. */
  readonly nickname = input.required<string>();

  protected readonly icons = Icons;
  protected readonly iconFor = achievementIcon;
  protected readonly profile = signal<PublicProfileDto | null>(null);
  protected readonly notFound = signal(false);
  protected readonly isMe = () => this.profile()?.nickname.toLowerCase() === this.auth.user()?.nickname.toLowerCase();

  constructor() {
    // Re-load when the nickname in the URL changes (e.g. clicking another player on the same page)
    effect(() => {
      const nickname = this.nickname();
      untracked(() => void this.load(nickname));
    });
  }

  private async load(nickname: string): Promise<void> {
    this.profile.set(null);
    this.notFound.set(false);
    try {
      this.profile.set(await firstValueFrom(this.api.getPublicQuietly(nickname)));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.notFound.set(true);
      }
    }
  }
}
