import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { BetsApi } from '../../core/api/bets.api';
import { GamesApi } from '../../core/api/games.api';
import { AuthStore } from '../../core/auth/auth.store';
import { BetDto, GameDto, RoundStateChangedEvent } from '../../core/models/api.models';
import { CrashHub } from '../../core/realtime/crash-hub';
import { WalletStore } from '../../core/state/wallet.store';
import { SoundService } from '../../core/ui/sound.service';
import { ToastStore } from '../../core/ui/toast.store';
import { SessionReminder } from '../limits/session-reminder';
import { CrashStore } from './crash.store';

const GAME: GameDto = {
  id: 1,
  name: 'Crash',
  slug: 'crash',
  description: null,
  type: 'Crash',
  status: 'Active',
  minBet: 1,
  maxBet: 10_000,
  bettingDurationSec: 10,
  pauseBetweenRoundsSec: 5,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null
};

const stateEvent = (roundId: number, state: RoundStateChangedEvent['state'], extra: Partial<RoundStateChangedEvent> = {}) => ({
  roundId,
  roundNumber: roundId,
  state,
  serverSeedHash: 'hash',
  bettingEndsAt: null,
  startedAt: null,
  serverTime: new Date().toISOString(),
  ...extra
});

const betDto = (overrides: Partial<BetDto> = {}): BetDto => ({
  id: 77,
  roundId: 5,
  roundNumber: 5,
  gameSlug: 'crash',
  amount: 10,
  slot: 1,
  autoCashOutAt: null,
  status: 'Active',
  cashOutMultiplier: null,
  payout: null,
  crashPoint: null,
  placedAt: '2026-10-25T10:00:00Z',
  cashedOutAt: null,
  ...overrides
});

describe('CrashStore', () => {
  let store: InstanceType<typeof CrashStore>;
  let betsApi: jasmine.SpyObj<BetsApi>;
  let gamesApi: jasmine.SpyObj<GamesApi>;
  let hub: jasmine.SpyObj<CrashHub>;
  let wallet: { setBalance: jasmine.Spy };
  let sound: { play: jasmine.Spy };

  beforeEach(() => {
    betsApi = jasmine.createSpyObj<BetsApi>('BetsApi', ['place', 'cashOut', 'my']);
    gamesApi = jasmine.createSpyObj<GamesApi>('GamesApi', ['getGame', 'currentRound', 'roundHistory', 'roundBets']);
    hub = jasmine.createSpyObj<CrashHub>('CrashHub', ['connect', 'disconnect']);
    hub.connect.and.resolveTo();
    wallet = { setBalance: jasmine.createSpy('setBalance') };
    sound = { play: jasmine.createSpy('play') };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        CrashStore,
        { provide: GamesApi, useValue: gamesApi },
        { provide: BetsApi, useValue: betsApi },
        { provide: CrashHub, useValue: hub },
        {
          provide: AuthStore,
          useValue: {
            isPlayer: signal(true),
            isLoggedIn: signal(true),
            user: signal({ id: 1, nickname: 'me', email: 'me@x.dev', role: 'Player', avatarUrl: null })
          }
        },
        { provide: WalletStore, useValue: wallet },
        { provide: ToastStore, useValue: jasmine.createSpyObj('ToastStore', ['success', 'info', 'error']) },
        { provide: SessionReminder, useValue: jasmine.createSpyObj('SessionReminder', ['recordBet']) },
        { provide: SoundService, useValue: sound }
      ]
    });

    store = TestBed.inject(CrashStore);
  });

  it('a new round clears the live bets of the previous one', () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(1, 'BettingOpen'));
    h.betPlaced({ roundId: 1, nickname: 'alice', amount: 5 });
    expect(store.liveBets().length).toBe(1);

    h.roundStateChanged(stateEvent(2, 'Waiting'));
    expect(store.liveBets()).toEqual([]);
    expect(store.round()?.id).toBe(2);
  });

  it('ignores bets of another round', () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(3, 'BettingOpen'));
    h.betPlaced({ roundId: 99, nickname: 'late', amount: 5 });
    expect(store.liveBets()).toEqual([]);
  });

  it('canBet only while betting is open and I have no bet yet', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'Waiting'));
    expect(store.canBet()).toBeFalse();

    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    expect(store.canBet()).toBeTrue();

    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);

    expect(store.myRoundBet()?.betId).toBe(77);
    expect(store.canBet()).toBeFalse();
    expect(wallet.setBalance).toHaveBeenCalledWith(990);
  });

  it('canCashOut only while running with an active bet', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);
    expect(store.canCashOut()).toBeFalse();

    h.roundStateChanged(stateEvent(5, 'Running', { startedAt: new Date().toISOString() }));
    expect(store.canCashOut()).toBeTrue();
  });

  it('crash: active bets are lost, the crash point is shown and added to the history', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);
    h.betPlaced({ roundId: 5, nickname: 'me', amount: 10 });
    h.roundStateChanged(stateEvent(5, 'Running', { startedAt: new Date().toISOString() }));

    h.roundCrashed({ roundId: 5, crashPoint: 1.42, serverSeed: 'seed' });

    expect(store.myRoundBet()?.status).toBe('Lost');
    expect(store.liveBets()[0].status).toBe('Lost');
    expect(store.history()[0]).toEqual({ roundId: 5, roundNumber: 5, crashPoint: 1.42, cancelled: false });
    expect(store.multiplier(performance.now(), Date.now())).toBe(1.42);
  });

  it('auto cash-out from the server updates my bet and the live list', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto({ autoCashOutAt: 2 }), balance: 990 }));
    await store.placeBet(10, 2);
    h.betPlaced({ roundId: 5, nickname: 'me', amount: 10 });
    h.betPlaced({ roundId: 5, nickname: 'bob', amount: 50 });

    h.playerCashedOut({ roundId: 5, nickname: 'me', multiplier: 2, payout: 20 });

    expect(store.myRoundBet()).toEqual(jasmine.objectContaining({ status: 'CashedOut', multiplier: 2, payout: 20 }));
    expect(store.liveBets().find((b) => b.nickname === 'me')?.status).toBe('CashedOut');
    expect(store.liveBets().find((b) => b.nickname === 'bob')?.status).toBe('Active');
  });

  it('sounds: a bet, and the crash sound only when MY bet is lost', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);
    expect(sound.play).toHaveBeenCalledWith('bet');

    h.roundStateChanged(stateEvent(5, 'Running'));
    h.roundCrashed({ roundId: 5, crashPoint: 1.5, serverSeed: 'seed' });
    expect(sound.play).toHaveBeenCalledWith('crash');

    // Watching a round without a bet: silence
    sound.play.calls.reset();
    h.roundStateChanged(stateEvent(6, 'Running'));
    h.roundCrashed({ roundId: 6, crashPoint: 1.2, serverSeed: 'seed' });
    expect(sound.play).not.toHaveBeenCalled();
  });

  it('a manual cash-out plays ONE sound, even when the live event arrives before the REST answer', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);
    h.betPlaced({ roundId: 5, nickname: 'me', amount: 10 });
    h.roundStateChanged(stateEvent(5, 'Running'));
    sound.play.calls.reset();

    // The server's PlayerCashedOut event wins the race, then the REST response arrives
    betsApi.cashOut.and.callFake(() => {
      h.playerCashedOut({ roundId: 5, nickname: 'me', multiplier: 1.8, payout: 18 });
      return of({ bet: betDto({ status: 'CashedOut', cashOutMultiplier: 1.8, payout: 18 }), balance: 1008 });
    });
    await store.cashOut();

    expect(sound.play.calls.allArgs()).toEqual([['cashout']]);
  });

  it('cancelled round: my bet and live bets are refunded', async () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'BettingOpen'));
    betsApi.place.and.returnValue(of({ bet: betDto(), balance: 990 }));
    await store.placeBet(10, null);
    h.betPlaced({ roundId: 5, nickname: 'me', amount: 10 });

    h.roundStateChanged(stateEvent(5, 'Cancelled'));

    expect(store.myRoundBet()?.status).toBe('Refunded');
    expect(store.liveBets()[0].status).toBe('Refunded');
    expect(store.history()[0].cancelled).toBeTrue();
  });

  it('extrapolates at most 250 ms past the last server tick', () => {
    const h = store.eventHandlers();
    h.roundStateChanged(stateEvent(5, 'Running', { startedAt: new Date().toISOString() }));
    h.multiplierTick({ roundId: 5, multiplier: 1.5, elapsedMs: 6_758 });

    const tickArrived = performance.now();
    // 10 s without new ticks (connection problem): the display must not keep flying
    expect(store.elapsedMs(tickArrived + 10_000, Date.now())).toBeCloseTo(6_758 + 250, 0);
  });

  it('countdown uses the server clock, also when joining in the middle of betting', () => {
    // The server clock is 60 s ahead of this computer, and we join 4 s after betting opened (6 s left)
    const serverNow = Date.now() + 60_000;
    store.eventHandlers().roundStateChanged(
      stateEvent(6, 'BettingOpen', {
        bettingEndsAt: new Date(serverNow + 6_000).toISOString(),
        serverTime: new Date(serverNow).toISOString()
      })
    );

    expect(store.clockOffsetMs()).toBeCloseTo(60_000, -2);
    expect(store.bettingSecondsLeft(Date.now())).toBeCloseTo(6, 0);
  });

  it('loads game, history and the current round on init, then connects to SignalR', async () => {
    gamesApi.getGame.and.returnValue(of(GAME));
    gamesApi.roundHistory.and.returnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }));
    gamesApi.currentRound.and.returnValue(throwError(() => new Error('404: no round yet')));

    await store.init('crash');

    expect(store.game()?.slug).toBe('crash');
    expect(store.loaded()).toBeTrue();
    expect(hub.connect).toHaveBeenCalledWith('crash', jasmine.any(Object));
  });
  it('loads the bets of the current round even when it is already over (list matches the bet panel)', async () => {
    gamesApi.getGame.and.returnValue(of(GAME));
    gamesApi.roundHistory.and.returnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }));
    gamesApi.currentRound.and.returnValue(
      of({
        id: 9,
        gameSlug: 'crash',
        roundNumber: 9,
        state: 'Finished',
        serverSeedHash: 'hash',
        serverSeed: 'seed',
        crashPoint: 2.38,
        currentMultiplier: null,
        createdAt: '2026-10-25T10:00:00Z',
        bettingEndsAt: null,
        startedAt: null,
        crashedAt: null,
        endedAt: null
      })
    );
    gamesApi.roundBets.and.returnValue(
      of([{ betId: 1, nickname: 'me', avatarUrl: null, amount: 10, slot: 1, status: 'CashedOut', cashOutMultiplier: 1.01, payout: 10.1, placedAt: '2026-10-25T10:00:00Z' }])
    );
    betsApi.my.and.returnValue(of({ items: [betDto({ roundId: 9, status: 'CashedOut', cashOutMultiplier: 1.01, payout: 10.1 })], page: 1, pageSize: 5, totalCount: 1, totalPages: 1 }));

    await store.init('crash');

    expect(store.liveBets().length).toBe(1);
    expect(store.liveBets()[0].status).toBe('CashedOut');
    expect(store.myRoundBet()?.status).toBe('CashedOut');
  });
});
