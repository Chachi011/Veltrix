import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AchievementsApi } from '../../core/api/achievements.api';
import { LocalDatePipe, ServerTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { AchievementKind, MyAchievementsDto } from '../../core/models/api.models';
import { ACHIEVEMENT_KIND_LABELS } from '../../shared/labels';
import { Badge, BadgeTone, Card, Icons, Skeleton, achievementIcon } from '../../shared/ui';

const KIND_TONES: Record<AchievementKind, BadgeTone> = { Safety: 'success', Learn: 'primary', Play: 'neutral' };

/**
 * The player's achievements on the profile page, safety and learning first. Each one is a single step (never a count
 * of bets or an amount). The server decides when one is completed and, while the player cannot bet (self-excluded or
 * daily limit used up), leaves out the ones that ask them to play; this component only shows what it gets.
 */
@Component({
  selector: 'vx-my-achievements',
  imports: [LucideAngularModule, Badge, Card, Skeleton, LocalDatePipe, ServerTextPipe, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-card [title]="'Achievements' | t" [subtitle]="'Learn the game and its safety tools. Each one gives XP and VLX.' | t">
      <!-- On its own: an @if with one root node can still be projected into the card's header slot -->
      @if (data(); as d) {
        <p card-actions class="num text-xs text-muted">{{ '{count} of {total} unlocked' | t: { count: unlocked(), total: d.items.length } }}</p>
      }
      @if (data(); as d) {
        @if (d.playTasksHidden) {
          <p class="mb-3 flex items-start gap-2 rounded-lg border border-border bg-surface-2 p-3 text-xs text-muted" role="note">
            <lucide-icon [img]="icons.pause" [size]="14" class="mt-0.5 shrink-0" aria-hidden="true" />
            {{ 'Achievements that need a bet are hidden while your betting is paused or your daily limit is used up.' | t }}
          </p>
        }
        <ul class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          @for (a of d.items; track a.code) {
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
                <div class="flex flex-wrap items-center gap-x-2 gap-y-1">
                  <p class="text-sm font-semibold" [class.text-muted]="!a.unlockedAt">{{ a.title | serverText }}</p>
                  <vx-badge [tone]="kindTones[a.kind]">{{ kindLabels[a.kind] | t }}</vx-badge>
                </div>
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
  protected readonly kindLabels = ACHIEVEMENT_KIND_LABELS;
  protected readonly kindTones = KIND_TONES;
  protected readonly data = signal<MyAchievementsDto | null>(null);
  protected readonly failed = signal(false);
  protected readonly unlocked = computed(() => this.data()?.items.filter((a) => a.unlockedAt !== null).length ?? 0);

  constructor() {
    firstValueFrom(this.api.mine()).then(
      (data) => this.data.set(data),
      () => this.failed.set(true)
    );
  }
}
