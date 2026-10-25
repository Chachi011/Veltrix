import { BetStatus, RoundState } from '../../../core/models/api.models';
import { MyBet } from '../crash.store';
import { betAction } from './bet-panel';

const bet = (status: BetStatus): MyBet => ({
  betId: 1,
  roundId: 5,
  amount: 10,
  autoCashOutAt: null,
  status,
  multiplier: null,
  payout: null
});

const player = (state: RoundState | null, myBet: MyBet | null = null) => ({ loggedIn: true, isPlayer: true, state, myBet });

describe('betAction (the one big button of the bet panel)', () => {
  it('guests are asked to log in, staff only watch', () => {
    expect(betAction({ loggedIn: false, isPlayer: false, state: 'BettingOpen', myBet: null })).toBe('login');
    expect(betAction({ loggedIn: true, isPlayer: false, state: 'BettingOpen', myBet: null })).toBe('staff');
  });

  it('"Place bet" only while betting is open', () => {
    expect(betAction(player('BettingOpen'))).toBe('place');
    for (const state of ['Waiting', 'BettingClosed', 'Running', 'Crashed', 'Finished', 'Cancelled', null] as const) {
      expect(betAction(player(state))).withContext(String(state)).toBe('closed');
    }
  });

  it('a placed bet waits until the round runs, then becomes "Cash out"', () => {
    expect(betAction(player('BettingOpen', bet('Active')))).toBe('waiting');
    expect(betAction(player('BettingClosed', bet('Active')))).toBe('waiting');
    expect(betAction(player('Running', bet('Active')))).toBe('cashout');
  });

  it('shows the result after the round', () => {
    expect(betAction(player('Running', bet('CashedOut')))).toBe('won');
    expect(betAction(player('Crashed', bet('Lost')))).toBe('lost');
    expect(betAction(player('Cancelled', bet('Refunded')))).toBe('refunded');
  });
});
