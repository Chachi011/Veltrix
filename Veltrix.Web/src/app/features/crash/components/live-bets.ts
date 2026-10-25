import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MultiplierPipe, VlxPipe } from '../../../shared/pipes';
import { LiveBet } from '../crash.store';

/** Everyone's bets in the current round, updated live by BetPlaced / PlayerCashedOut / RoundCrashed. */
@Component({
  selector: 'vx-live-bets',
  imports: [VlxPipe, MultiplierPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card" aria-label="Live bets">
      <div class="head">
        <h2>Live bets</h2>
        <span class="muted num">{{ bets().length }} · {{ total() | vlx }}</span>
      </div>

      @if (sorted().length === 0) {
        <p class="empty">No bets in this round yet.</p>
      } @else {
        <ul class="list">
          @for (bet of sorted(); track bet.key) {
            <li [class]="bet.status">
              <span class="nick" [class.me]="bet.nickname === me()">{{ bet.nickname }}</span>
              <span class="num amount">{{ bet.amount | vlx: false }}</span>
              <span class="num mult">{{ bet.status === 'CashedOut' ? (bet.multiplier | mult) : '' }}</span>
              <span class="num payout">
                @switch (bet.status) {
                  @case ('CashedOut') { +{{ bet.payout | vlx: false }} }
                  @case ('Lost') { −{{ bet.amount | vlx: false }} }
                  @case ('Refunded') { refunded }
                  @default { … }
                }
              </span>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    .head { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
    .list { list-style: none; margin: 0; padding: 0; max-height: 360px; overflow-y: auto; }
    li {
      display: grid;
      grid-template-columns: 1fr auto 56px 84px;
      gap: 8px;
      padding: 8px 4px;
      border-bottom: 1px solid var(--border);
      font-size: 0.9rem;
    }
    li:last-child { border-bottom: 0; }
    .nick { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .me { color: var(--primary); font-weight: 700; }
    .amount, .mult, .payout { text-align: right; }
    .CashedOut .mult, .CashedOut .payout { color: var(--success); font-weight: 600; }
    .Lost { opacity: 0.55; }
    .Lost .payout { color: var(--danger); }
    .Refunded .payout { color: var(--text-muted); }
  `
})
export class LiveBets {
  readonly bets = input.required<LiveBet[]>();
  readonly me = input<string | null>(null);

  protected readonly total = computed(() => this.bets().reduce((sum, b) => sum + b.amount, 0));

  /** Cashed-out players on top (newest win first), then the rest by bet size. */
  protected readonly sorted = computed(() =>
    [...this.bets()].sort((a, b) => {
      const rank = (x: LiveBet) => (x.status === 'CashedOut' ? 0 : 1);
      return rank(a) - rank(b) || b.amount - a.amount;
    })
  );
}
