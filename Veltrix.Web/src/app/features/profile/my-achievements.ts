import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AchievementsApi } from '../../core/api/achievements.api';
import { LocalDatePipe, ServerTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { MyAchievementDto } from '../../core/models/api.models';
import { Card, Icons, ProgressBar, Skeleton, achievementIcon } from '../../shared/ui';

/**
 * The player's achievements on the profile page: unlocked ones with the date, locked ones with their progress
 * (e.g. 37 / 100 bets). The server decides when one is earned, during round settlement; this only shows it.
 */
@Component({
  selector: 'vx-my-achievements',
  imports: [LucideAngularModule, Card, ProgressBar, Skeleton, LocalDatePipe, ServerTextPipe, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-card [title]="'Achievements' | t" [subtitle]="'Unlocked automatically after a round. Each one gives XP and VLX.' | t">
      <!-- On its own: an @if with one root node can still be projected into the card's header slot -->
      @if (items(); as list) {
        <p card-actions class="num text-xs text-muted">{{ '{count} of {total} unlocked' | t: { count: unlocked(), total: list.length } }}</p>
      }
      @if (items(); as list) {
        <ul class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          @for (a of list; track a.code) {
            <li
              class="flex gap-3 rounded-lg border p-3"
              [class]="a.unlockedAt ? 'border-accent/30 bg-accent/5' : 'border-border bg-surface'"
            >
              <span
                class="flex size-10 shrink-0 items-center justify-center rounded-full"
                [class]="a.unlockedAt ? 'bg-accent/15 text-accent' : 'bg-surface-2 text-muted'"
                aria-hidden="true"
              >
                <lucide-icon [img]="iconFor(a.icon)" [size]="18" />
              </span>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-semibold" [class.text-muted]="!a.unlockedAt">{{ a.title | serverText }}</p>
                <p class="mt-0.5 text-xs text-muted">{{ a.description | serverText }}</p>
                <p class="num mt-1.5 text-xs font-medium text-accent">+{{ a.xpReward }} XP · +{{ a.vlxReward }} VLX</p>
                @if (a.unlockedAt) {
                  <p class="mt-1 flex items-center gap-1 text-xs text-success">
                    <lucide-icon [img]="icons.success" [size]="12" aria-hidden="true" />
                    {{ 'Unlocked {date}' | t: { date: (a.unlockedAt | vxDate: 'mediumDate') } }}
                  </p>
                } @else if (a.target && (a.progress ?? 0) >= a.target) {
                  <!-- Goal met before achievements existed (older accounts): the award comes with the next settled bet -->
                  <p class="mt-1 text-xs text-accent">{{ 'Unlocks when your next bet is settled' | t }}</p>
                } @else if (a.target) {
                  <vx-progress-bar class="mt-2" [value]="(a.progress ?? 0) / a.target" [label]="a.title | serverText" />
                  <p class="num mt-1 text-xs text-muted">{{ a.progress ?? 0 }} / {{ a.target }}</p>
                } @else {
                  <p class="mt-1 text-xs text-muted">{{ 'Not unlocked yet' | t }}</p>
                }
              </div>
            </li>
          }
        </ul>
      } @else if (failed()) {
        <p class="text-sm text-muted">{{ 'Achievements could not be loaded.' | t }}</p>
      } @else {
        <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          @for (row of [1, 2, 3]; track row) {
            <vx-skeleton height="88px" />
          }
        </div>
      }
    </vx-card>
  `
})
export class MyAchievements {
  private readonly api = inject(AchievementsApi);

  protected readonly icons = Icons;
  protected readonly iconFor = achievementIcon;
  protected readonly items = signal<MyAchievementDto[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly unlocked = computed(() => this.items()?.filter((a) => a.unlockedAt !== null).length ?? 0);

  constructor() {
    firstValueFrom(this.api.mine()).then(
      (list) => this.items.set(list),
      () => this.failed.set(true)
    );
  }
}
