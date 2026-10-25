import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GameDto, RoundState } from '../../../core/models/api.models';
import { FieldError } from '../../../shared/field-error';
import { MultiplierPipe, VlxPipe } from '../../../shared/pipes';
import { twoDecimals } from '../../../shared/validators';
import { MyBet } from '../crash.store';

export interface BetRequest {
  amount: number;
  autoCashOutAt: number | null;
}

/** Bet form, Cash out button and the result of my bet in the current round. */
@Component({
  selector: 'vx-bet-panel',
  imports: [ReactiveFormsModule, RouterLink, FieldError, VlxPipe, MultiplierPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bet-panel.html',
  styleUrl: './bet-panel.scss'
})
export class BetPanel {
  readonly game = input<GameDto | null>(null);
  readonly balance = input<number | null>(null);
  readonly state = input<RoundState | null>(null);
  readonly myBet = input<MyBet | null>(null);
  readonly multiplier = input(1);
  readonly loggedIn = input(false);
  readonly isPlayer = input(false);
  readonly canBet = input(false);
  readonly canCashOut = input(false);
  readonly placing = input(false);
  readonly cashingOut = input(false);

  readonly bet = output<BetRequest>();
  readonly cashOut = output<void>();

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
      return;
    }
    this.bet.emit({ amount: Number(amount), autoCashOutAt: autoEnabled ? Number(autoCashOutAt) : null });
  }

  protected waitingLabel(): string {
    switch (this.state()) {
      case 'Running':
        return 'Round in progress, wait for the next one';
      case 'BettingClosed':
        return 'Betting closed';
      case 'Crashed':
      case 'Finished':
      case 'Cancelled':
      case 'Waiting':
        return 'Next round starting soon';
      default:
        return 'Connecting…';
    }
  }
}
