import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/i18n-pipes';
import { MultiplierPill, Skeleton } from '../../../shared/ui';
import { HistoryItem } from '../crash.store';

/** Crash points of the last 20 rounds, newest first. Each pill opens that round's provably fair check. */
@Component({
  selector: 'vx-history-strip',
  imports: [RouterLink, MultiplierPill, Skeleton, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-w-0 items-center gap-3' },
  template: `
    <span class="shrink-0 text-xs font-medium text-muted">{{ 'Last 20' | t }}</span>
    <div class="flex min-w-0 flex-1 gap-1.5 overflow-x-auto pb-1 [scrollbar-width:thin]" [attr.aria-label]="'Crash points of the last rounds' | t">
      @if (loading()) {
        @for (i of placeholders; track i) {
          <vx-skeleton width="56px" height="28px" shape="circle" />
        }
      } @else {
        @for (item of items(); track item.roundId) {
          <a
            routerLink="/fairness"
            [queryParams]="{ round: item.roundId }"
            class="rounded-full"
            [title]="'Round #{number}: verify' | t: { number: item.roundNumber }"
            [attr.aria-label]="item.cancelled ? ('Round {number}, cancelled, verify' | t: { number: item.roundNumber }) : ('Round {number}, {multiplier}x, verify' | t: { number: item.roundNumber, multiplier: item.crashPoint ?? '—' })"
          >
            <vx-multiplier-pill [value]="item.crashPoint" [cancelled]="item.cancelled" class="hover:border-primary/50" />
          </a>
        } @empty {
          <span class="text-xs text-muted">{{ 'No finished rounds yet.' | t }}</span>
        }
      }
    </div>
  `
})
export class HistoryStrip {
  readonly items = input.required<HistoryItem[]>();
  readonly loading = input(false);

  protected readonly placeholders = Array.from({ length: 12 }, (_, i) => i);
}
