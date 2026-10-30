import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { KnownTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { GameDto } from '../../core/models/api.models';
import { LimitsStore } from '../../core/state/limits.store';
import { GAME_STATUS_LABELS } from '../../shared/labels';
import { VlxPipe } from '../../shared/pipes';
import { Badge, BadgeTone, Button, Icons } from '../../shared/ui';

const STATUS_TONES: Record<GameDto['status'], BadgeTone> = { Active: 'success', Maintenance: 'warning', Disabled: 'neutral' };

/** One game in the catalog (and on the home page): name, status, bet range, round timing, Play. */
@Component({
  selector: 'vx-game-card',
  imports: [RouterLink, LucideAngularModule, VlxPipe, Badge, Button, TranslatePipe, KnownTextPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-w-0 flex-col rounded-card border border-border bg-surface p-5 shadow-card' },
  template: `
    <div class="flex items-start gap-3">
      <span class="grid size-11 shrink-0 place-items-center rounded-control border border-primary/20 bg-primary/12 text-primary-text">
        <lucide-icon [img]="icons.rocket" [size]="20" aria-hidden="true" />
      </span>
      <div class="min-w-0 flex-1">
        <h3 class="truncate text-[15px] font-semibold">{{ game().name }}</h3>
        <p class="text-xs text-muted">{{ game().type }}</p>
      </div>
      <vx-badge [tone]="statusTone()" dot>{{ statusLabels[game().status] | t }}</vx-badge>
    </div>

    <p class="mt-3 line-clamp-2 min-h-10 text-[13px] text-muted">{{ (game().description | knownText) || ('Cash out before the multiplier crashes.' | t) }}</p>

    <dl class="mt-4 grid grid-cols-2 gap-3 text-[13px]">
      <div>
        <dt class="text-xs text-muted">{{ 'Bet range' | t }}</dt>
        <dd class="num font-medium">{{ game().minBet | vlx: false }} – {{ game().maxBet | vlx: false }}</dd>
      </div>
      <div>
        <dt class="text-xs text-muted">{{ 'Betting time' | t }}</dt>
        <dd class="num font-medium">{{ '{count}s' | t: { count: game().bettingDurationSec } }}</dd>
      </div>
    </dl>

    <div class="mt-5">
      <!-- Responsible play: a player who cannot bet right now is not invited to play, only to watch -->
      @if (game().status === 'Active' && !limits.canPlayNow()) {
        <a vxButton [routerLink]="link()" variant="secondary" block>{{ 'Watch' | t }}</a>
      } @else if (game().status === 'Active') {
        <a vxButton [routerLink]="link()" variant="primary" block>
          <lucide-icon [img]="icons.rocket" [size]="16" aria-hidden="true" />{{ 'Play' | t }}
        </a>
      } @else {
        <a vxButton [routerLink]="link()" variant="secondary" block>{{
          game().status === 'Maintenance' ? ('Watch (maintenance)' | t) : ('Watch (disabled)' | t)
        }}</a>
      }
    </div>
  `
})
export class GameCard {
  readonly game = input.required<GameDto>();

  protected readonly limits = inject(LimitsStore);
  protected readonly icons = Icons;
  protected readonly statusLabels = GAME_STATUS_LABELS;
  protected readonly statusTone = computed(() => STATUS_TONES[this.game().status]);
  /** The main game keeps its short URL. */
  protected readonly link = computed(() => (this.game().slug === 'crash' ? '/crash' : `/crash/${this.game().slug}`));
}
