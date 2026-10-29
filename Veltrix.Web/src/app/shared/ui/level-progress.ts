import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ServerTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { PublicProfileDto } from '../../core/models/api.models';
import { ProgressBar } from './progress-bar';

/**
 * Level with its title and a bar from the current level's XP to the next one (spec 4.6: XP levels).
 * The numbers come from the server (PublicProfileDto); the client only draws them.
 */
@Component({
  selector: 'vx-level-progress',
  imports: [ProgressBar, ServerTextPipe, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div class="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
      <p class="text-sm font-semibold">
        {{ 'Level {level}' | t: { level: profile().level } }}
        <span class="font-normal text-muted">· {{ profile().levelTitle | serverText }}</span>
      </p>
      <p class="num text-xs text-muted">{{ '{xp} XP' | t: { xp: profile().xp } }}</p>
    </div>
    <vx-progress-bar class="mt-2" tone="accent" [value]="fraction()" [label]="'Progress to the next level' | t" />
    <p class="mt-1.5 text-xs text-muted">
      @if (profile().nextLevel !== null) {
        {{ '{xp} XP to level {level}' | t: { xp: remaining(), level: profile().nextLevel! } }}
        · {{ profile().nextLevelTitle | serverText }}
      } @else {
        {{ 'Top level reached' | t }}
      }
    </p>
  `
})
export class LevelProgress {
  readonly profile = input.required<PublicProfileDto>();

  /** 0..1 inside the current level; full at the top level. */
  protected readonly fraction = computed(() => {
    const p = this.profile();
    if (p.nextLevelXp === null) {
      return 1;
    }
    const span = p.nextLevelXp - p.levelXp;
    return span > 0 ? (p.xp - p.levelXp) / span : 1;
  });

  protected readonly remaining = computed(() => {
    const p = this.profile();
    return p.nextLevelXp === null ? 0 : Math.max(0, p.nextLevelXp - p.xp);
  });
}
