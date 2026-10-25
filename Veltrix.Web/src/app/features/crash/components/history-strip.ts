import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HistoryItem } from '../crash.store';

/** Crash points of the last 20 rounds. Each one links to its provably fair check. */
@Component({
  selector: 'vx-history-strip',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="strip" aria-label="Last rounds">
      @for (item of items(); track item.roundId) {
        <a
          class="pill num"
          [class]="'pill num ' + tier(item)"
          routerLink="/fairness"
          [queryParams]="{ round: item.roundId }"
          [title]="'Round #' + item.roundNumber + ': verify'"
        >
          {{ item.cancelled ? '—' : item.crashPoint?.toFixed(2) + 'x' }}
        </a>
      } @empty {
        <span class="muted">No finished rounds yet.</span>
      }
    </div>
  `,
  styles: `
    .strip {
      display: flex;
      gap: 6px;
      overflow-x: auto;
      padding-bottom: 4px;
      scrollbar-width: thin;
    }
    .pill {
      flex: 0 0 auto;
      padding: 4px 10px;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 700;
      background: var(--surface-2);
      border: 1px solid var(--border);
      color: var(--text);
    }
    .pill:hover { text-decoration: none; border-color: var(--primary); }
    .low { color: var(--danger); }
    .mid { color: var(--success); }
    .high { color: var(--gold); border-color: var(--gold); }
    .cancelled { color: var(--text-muted); }
  `
})
export class HistoryStrip {
  readonly items = input.required<HistoryItem[]>();

  protected tier(item: HistoryItem): string {
    if (item.cancelled || item.crashPoint === null) return 'cancelled';
    if (item.crashPoint < 2) return 'low';
    if (item.crashPoint < 10) return 'mid';
    return 'high';
  }
}
