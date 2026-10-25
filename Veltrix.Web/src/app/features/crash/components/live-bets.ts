import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Avatar, Card, EmptyState, Icons } from '../../../shared/ui';
import { MultiplierPipe, VlxPipe } from '../../../shared/pipes';
import { LiveBet } from '../crash.store';

/** Everyone's bets in the current round, updated live by BetPlaced / PlayerCashedOut / RoundCrashed. */
@Component({
  selector: 'vx-live-bets',
  imports: [Avatar, Card, EmptyState, VlxPipe, MultiplierPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-card title="Live bets" padding="none">
      <span card-actions class="num text-xs text-muted">{{ bets().length }} · {{ total() | vlx }}</span>

      @if (sorted().length === 0) {
        <vx-empty-state [icon]="icons.users" title="No bets yet" message="Bets placed in this round appear here live." />
      } @else {
        <div class="vx-table-wrap mt-2 max-h-[420px] overflow-y-auto">
          <table class="vx-table vx-table-compact">
            <thead>
              <tr>
                <th>Player</th>
                <th class="text-right">Bet</th>
                <th class="text-right">At</th>
                <th class="text-right">Payout</th>
              </tr>
            </thead>
            <tbody>
              @for (bet of sorted(); track bet.key) {
                <tr [class]="rowClass(bet)">
                  <td>
                    <span class="flex max-w-[130px] items-center gap-2">
                      <vx-avatar [name]="bet.nickname" size="xs" />
                      <span class="truncate font-medium" [class.text-primary-text]="bet.nickname === me()" [title]="bet.nickname === me() ? 'You' : bet.nickname">
                        {{ bet.nickname }}
                      </span>
                    </span>
                  </td>
                  <td class="num text-right">{{ bet.amount | vlx: false }}</td>
                  <td class="num text-right" [class.text-success]="bet.status === 'CashedOut'">
                    {{ bet.status === 'CashedOut' ? (bet.multiplier | mult) : '—' }}
                  </td>
                  <td class="num text-right font-semibold">
                    @switch (bet.status) {
                      @case ('CashedOut') {
                        <span class="text-success">+{{ bet.payout | vlx: false }}</span>
                      }
                      @case ('Lost') {
                        <span class="text-danger">−{{ bet.amount | vlx: false }}</span>
                      }
                      @case ('Refunded') {
                        <span class="text-xs font-normal text-muted">refunded</span>
                      }
                      @default {
                        <span class="text-muted">…</span>
                      }
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </vx-card>
  `
})
export class LiveBets {
  readonly bets = input.required<LiveBet[]>();
  readonly me = input<string | null>(null);

  protected readonly icons = Icons;
  protected readonly total = computed(() => this.bets().reduce((sum, b) => sum + b.amount, 0));

  /** Cashed-out players on top, then the rest by bet size. */
  protected readonly sorted = computed(() =>
    [...this.bets()].sort((a, b) => {
      const rank = (x: LiveBet) => (x.status === 'CashedOut' ? 0 : 1);
      return rank(a) - rank(b) || b.amount - a.amount;
    })
  );

  protected rowClass(bet: LiveBet): string {
    if (bet.status === 'CashedOut') return 'bg-success/5';
    if (bet.status === 'Lost') return 'opacity-60';
    return '';
  }
}
