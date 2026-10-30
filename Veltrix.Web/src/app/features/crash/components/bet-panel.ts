import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { LocalDatePipe, TranslatePipe, TranslateSplitPipe } from '../../../core/i18n/i18n-pipes';
import { GameDto, RoundState } from '../../../core/models/api.models';
import { Button, Field, Icons } from '../../../shared/ui';
import { MultiplierPipe, VlxPipe } from '../../../shared/pipes';
import { twoDecimals } from '../../../shared/validators';
import { MyBet } from '../crash.store';

export interface BetRequest {
  amount: number;
  autoCashOutAt: number | null;
}

/** What the ONE big button of the panel shows. */
export type BetAction =
  | 'login' // guest: link to log in
  | 'staff' // Operator / Admin: watching only
  | 'place' // betting is open and I have no bet yet
  | 'closed' // no bet, betting is not open
  | 'paused' // self-excluded (responsible play): no new bets until the pause ends
  | 'limit' // today's daily limit is used up (responsible play): no new bets until the day resets
  | 'waiting' // bet placed, round not started yet
  | 'cashout' // round running, my bet is active
  | 'won'
  | 'lost'
  | 'refunded';

export interface BetActionInput {
  loggedIn: boolean;
  isPlayer: boolean;
  state: RoundState | null;
  myBet: MyBet | null;
  /** Self-excluded: a bet already in the round can still be cashed out, but no new bet. */
  paused?: boolean;
  /** Daily limit used up: same as paused, until the day resets. */
  limitReached?: boolean;
}

/** "Place bet" → "Waiting…" → "Cash out 13.50 VLX" → result. A pure function, so it is unit-tested. */
export function betAction({ loggedIn, isPlayer, state, myBet, paused = false, limitReached = false }: BetActionInput): BetAction {
  if (!loggedIn) return 'login';
  if (!isPlayer) return 'staff';
  if (!myBet) return paused ? 'paused' : limitReached ? 'limit' : state === 'BettingOpen' ? 'place' : 'closed';

  switch (myBet.status) {
    case 'Active':
      return state === 'Running' ? 'cashout' : 'waiting';
    case 'CashedOut':
      return 'won';
    case 'Lost':
      return 'lost';
    case 'Refunded':
      return 'refunded';
  }
}

/**
 * Bet form + the state-changing action button. Desktop: a card in the right column.
 * Phones: a bottom sheet above the bottom nav; the form folds away, the button stays reachable.
 */
@Component({
  selector: 'vx-bet-panel',
  imports: [ReactiveFormsModule, RouterLink, LucideAngularModule, Button, Field, VlxPipe, MultiplierPipe, TranslatePipe, TranslateSplitPipe, LocalDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bet-panel.html'
})
export class BetPanel {
  readonly game = input<GameDto | null>(null);
  readonly balance = input<number | null>(null);
  readonly state = input<RoundState | null>(null);
  readonly myBet = input<MyBet | null>(null);
  readonly multiplier = input(1);
  readonly crashPoint = input<number | null>(null);
  readonly loggedIn = input(false);
  readonly isPlayer = input(false);
  /** Self-excluded until this time (null = not paused). */
  readonly pausedUntil = input<string | null>(null);
  /** Daily limit used up until this time (null = can still bet today). */
  readonly limitResetsAt = input<string | null>(null);
  readonly canBet = input(false);
  readonly canCashOut = input(false);
  readonly placing = input(false);
  readonly cashingOut = input(false);

  readonly bet = output<BetRequest>();
  readonly cashOut = output<void>();

  protected readonly icons = Icons;

  /** Phones only: is the bottom sheet unfolded? (Desktop always shows everything.) */
  protected readonly expanded = signal(false);

  protected readonly action = computed(() =>
    betAction({
      loggedIn: this.loggedIn(),
      isPlayer: this.isPlayer(),
      state: this.state(),
      myBet: this.myBet(),
      paused: this.pausedUntil() !== null,
      limitReached: this.limitResetsAt() !== null
    })
  );

  /** Form fields are locked once a bet is in the round (or for guests / staff). */
  protected readonly locked = computed(
    () => !this.loggedIn() || !this.isPlayer() || this.myBet() !== null || this.pausedUntil() !== null
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    amount: [10, [Validators.required, twoDecimals]],
    autoEnabled: [false],
    autoCashOutAt: [2, [Validators.required, Validators.min(1.01), Validators.max(1_000_000), twoDecimals]]
  });

  /** What a cash-out would pay right now (the real amount is decided by the server). */
  protected readonly potentialPayout = computed(() => {
    const bet = this.myBet();
    return bet ? Math.floor(bet.amount * this.multiplier() * 100) / 100 : 0;
  });

  protected readonly maxAmount = computed(() => {
    const max = this.game()?.maxBet ?? 0;
    const balance = this.balance();
    return balance === null ? max : Math.min(max, balance);
  });

  constructor() {
    // The allowed range depends on the game settings and the current balance
    effect(() => {
      const min = this.game()?.minBet ?? 1;
      const max = this.maxAmount();
      const amount = this.form.controls.amount;
      amount.setValidators([Validators.required, Validators.min(min), Validators.max(Math.max(min, max)), twoDecimals]);
      amount.updateValueAndValidity({ emitEvent: false });
    });

    effect(() => (this.locked() ? this.form.disable({ emitEvent: false }) : this.form.enable({ emitEvent: false })));
  }

  protected setAmount(kind: 'half' | 'double' | 'min' | 'max'): void {
    const control = this.form.controls.amount;
    const current = Number(control.value) || 0;
    const min = this.game()?.minBet ?? 1;
    const max = Math.max(min, this.maxAmount());

    const next = { half: current / 2, double: current * 2, min, max }[kind];
    control.setValue(Math.min(max, Math.max(min, Math.floor(next * 100) / 100)));
    control.markAsTouched();
  }

  protected submit(): void {
    const { amount, autoEnabled, autoCashOutAt } = this.form.getRawValue();
    const amountInvalid = this.form.controls.amount.invalid;
    const autoInvalid = autoEnabled && this.form.controls.autoCashOutAt.invalid;

    if (amountInvalid || autoInvalid) {
      this.form.markAllAsTouched();
      this.expanded.set(true);
      return;
    }
    this.bet.emit({ amount: Number(amount), autoCashOutAt: autoEnabled ? Number(autoCashOutAt) : null });
  }
}
