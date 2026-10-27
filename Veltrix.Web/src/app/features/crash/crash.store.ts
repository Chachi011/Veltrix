import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { BetsApi } from '../../core/api/bets.api';
import { GamesApi } from '../../core/api/games.api';
import { AuthStore } from '../../core/auth/auth.store';
import {
  BalanceUpdatedEvent,
  BetDto,
  BetPlacedEvent,
  BetStatus,
  GameDto,
  MultiplierTickEvent,
  OnlineCountEvent,
  PlayerCashedOutEvent,
  RoundCrashedEvent,
  RoundDto,
  RoundState,
  RoundStateChangedEvent
} from '../../core/models/api.models';
import { CrashHub, CrashHubHandlers } from '../../core/realtime/crash-hub';
import { WalletStore } from '../../core/state/wallet.store';
import { SessionReminder } from '../limits/session-reminder';
import { ToastStore } from '../../core/ui/toast.store';
import { elapsedFor, multiplierAt } from './crash-math';

export const HISTORY_SIZE = 20;

/** The client may run ahead of the last server tick by at most this much (so it never "flies" far past a crash). */
const MAX_EXTRAPOLATION_MS = 250;

export interface RoundView {
  id: number;
  roundNumber: number;
  state: RoundState;
  serverSeedHash: string;
  bettingEndsAt: string | null;
  startedAt: string | null;
}

export interface HistoryItem {
  roundId: number;
  roundNumber: number;
  crashPoint: number | null;
  cancelled: boolean;
}

export interface LiveBet {
  key: number;
  nickname: string;
  amount: number;
  status: BetStatus;
  multiplier: number | null;
  payout: number | null;
}

export interface MyBet {
  betId: number;
  roundId: number;
  amount: number;
  autoCashOutAt: number | null;
  status: BetStatus;
  multiplier: number | null;
  payout: number | null;
}

type CrashState = {
  slug: string;
  game: GameDto | null;
  round: RoundView | null;
  /** Last server tick and the local moment (performance.now()) it arrived. */
  tick: { elapsedMs: number; receivedAt: number } | null;
  crashPoint: number | null;
  serverSeed: string | null;
  history: HistoryItem[];
  liveBets: LiveBet[];
  myBet: MyBet | null;
  online: number;
  /** serverTime ≈ Date.now() + clockOffsetMs (the user's clock may be wrong). */
  clockOffsetMs: number;
  placing: boolean;
  cashingOut: boolean;
  loaded: boolean;
};

const initialState: CrashState = {
  slug: 'crash',
  game: null,
  round: null,
  tick: null,
  crashPoint: null,
  serverSeed: null,
  history: [],
  liveBets: [],
  myBet: null,
  online: 0,
  clockOffsetMs: 0,
  placing: false,
  cashingOut: false,
  loaded: false
};

const toRoundView = (r: RoundDto | RoundStateChangedEvent): RoundView => ({
  id: 'id' in r ? r.id : r.roundId,
  roundNumber: r.roundNumber,
  state: r.state,
  serverSeedHash: r.serverSeedHash,
  bettingEndsAt: r.bettingEndsAt,
  startedAt: r.startedAt
});

const toMyBet = (bet: BetDto): MyBet => ({
  betId: bet.id,
  roundId: bet.roundId,
  amount: bet.amount,
  autoCashOutAt: bet.autoCashOutAt,
  status: bet.status,
  multiplier: bet.cashOutMultiplier,
  payout: bet.payout
});

/**
 * State of the live Crash game. Provided by the Crash page (one instance per visit).
 * SignalR events and REST responses only ever flow INTO this store; components just read its signals.
 */
export const CrashStore = signalStore(
  withState(initialState),
  withComputed((store, auth = inject(AuthStore)) => {
    const state = computed(() => store.round()?.state ?? null);
    const myRoundBet = computed(() => {
      const bet = store.myBet();
      return bet && bet.roundId === store.round()?.id ? bet : null;
    });

    return {
      state,
      myRoundBet,
      canBet: computed(
        () => auth.isPlayer() && state() === 'BettingOpen' && myRoundBet() === null && !store.placing()
      ),
      canCashOut: computed(
        () => state() === 'Running' && myRoundBet()?.status === 'Active' && !store.cashingOut()
      ),
      totalBet: computed(() => store.liveBets().reduce((sum, b) => sum + b.amount, 0))
    };
  }),
  withMethods(
    (
      store,
      gamesApi = inject(GamesApi),
      betsApi = inject(BetsApi),
      hub = inject(CrashHub),
      auth = inject(AuthStore),
      wallet = inject(WalletStore),
      toasts = inject(ToastStore),
      reminder = inject(SessionReminder)
    ) => {
      let nextKey = 1;

      const addToHistory = (item: HistoryItem) =>
        patchState(store, (s) =>
          s.history[0]?.roundId === item.roundId ? {} : { history: [item, ...s.history].slice(0, HISTORY_SIZE) }
        );

      /** Load everything about the round in progress (on first visit and after a reconnect). */
      const loadCurrentRound = async () => {
        const slug = store.slug();
        const round = await firstValueFrom(gamesApi.currentRound(slug)).catch(() => null);
        if (!round) {
          return;
        }

        // Also for a round that is already over: the live list must match the result shown in the bet panel
        const bets = await firstValueFrom(gamesApi.roundBets(round.id)).catch(() => []);

        patchState(store, {
          round: toRoundView(round),
          crashPoint: round.crashPoint,
          serverSeed: round.serverSeed,
          // Joining mid-flight: the server's current multiplier tells us how long the round has been running
          tick:
            round.state === 'Running' && round.currentMultiplier
              ? { elapsedMs: elapsedFor(round.currentMultiplier), receivedAt: performance.now() }
              : null,
          liveBets: bets.map((b) => ({
            key: nextKey++,
            nickname: b.nickname,
            amount: b.amount,
            status: b.status,
            multiplier: b.cashOutMultiplier,
            payout: b.payout
          }))
        });

        if (auth.isPlayer()) {
          const mine = await firstValueFrom(betsApi.my({ page: 1, pageSize: 5 })).catch(() => null);
          const current = mine?.items.find((b) => b.roundId === round.id);
          patchState(store, { myBet: current ? toMyBet(current) : null });
        }
      };

      const handlers: CrashHubHandlers = {
        roundStateChanged: (e) => {
          const previous = store.round();
          const isNewRound = previous?.id !== e.roundId;

          patchState(store, (s) => ({
            round: toRoundView(e),
            ...(isNewRound ? { liveBets: [], crashPoint: null, serverSeed: null, tick: null } : {}),
            // The round started "now" on the server; ticks will correct this within 100 ms
            ...(e.state === 'Running' ? { tick: { elapsedMs: 0, receivedAt: performance.now() } } : {}),
            // How far this computer's clock is from the server's (used by the betting countdown)
            ...(e.serverTime ? { clockOffsetMs: Date.parse(e.serverTime) - Date.now() } : {}),
            ...(e.state === 'Cancelled'
              ? {
                  liveBets: s.liveBets.map((b) => (b.status === 'Active' ? { ...b, status: 'Refunded' as const } : b)),
                  myBet:
                    s.myBet?.roundId === e.roundId && s.myBet.status === 'Active'
                      ? { ...s.myBet, status: 'Refunded' as const, payout: s.myBet.amount }
                      : s.myBet
                }
              : {})
          }));

          if (e.state === 'Cancelled') {
            addToHistory({ roundId: e.roundId, roundNumber: e.roundNumber, crashPoint: null, cancelled: true });
            if (store.myBet()?.roundId === e.roundId) {
              toasts.info('The round was cancelled. Your bet was refunded.');
            }
          }
        },

        multiplierTick: (e) => {
          if (e.roundId === store.round()?.id) {
            patchState(store, { tick: { elapsedMs: e.elapsedMs, receivedAt: performance.now() } });
          }
        },

        roundCrashed: (e) => {
          const round = store.round();
          patchState(store, (s) => ({
            crashPoint: e.crashPoint,
            serverSeed: e.serverSeed,
            round: round && round.id === e.roundId ? { ...round, state: 'Crashed' as const } : round,
            liveBets: s.liveBets.map((b) => (b.status === 'Active' ? { ...b, status: 'Lost' as const, payout: 0 } : b)),
            myBet:
              s.myBet?.roundId === e.roundId && s.myBet.status === 'Active'
                ? { ...s.myBet, status: 'Lost' as const, payout: 0 }
                : s.myBet
          }));
          addToHistory({
            roundId: e.roundId,
            roundNumber: round?.roundNumber ?? 0,
            crashPoint: e.crashPoint,
            cancelled: false
          });
        },

        betPlaced: (e) => {
          if (e.roundId === store.round()?.id) {
            patchState(store, (s) => ({
              liveBets: [
                ...s.liveBets,
                { key: nextKey++, nickname: e.nickname, amount: e.amount, status: 'Active' as const, multiplier: null, payout: null }
              ]
            }));
          }
        },

        playerCashedOut: (e) => {
          if (e.roundId !== store.round()?.id) {
            return;
          }

          patchState(store, (s) => {
            // The active bet of this player whose amount matches payout / multiplier best
            const candidates = s.liveBets.filter((b) => b.nickname === e.nickname && b.status === 'Active');
            const target = candidates.sort(
              (a, b) => Math.abs(a.amount * e.multiplier - e.payout) - Math.abs(b.amount * e.multiplier - e.payout)
            )[0];

            const isMine = e.nickname === auth.user()?.nickname && s.myBet?.roundId === e.roundId;
            return {
              liveBets: s.liveBets.map((b) =>
                b === target ? { ...b, status: 'CashedOut' as const, multiplier: e.multiplier, payout: e.payout } : b
              ),
              // Auto cash-out happened on the server: show it in my bet panel too
              myBet:
                isMine && s.myBet?.status === 'Active'
                  ? { ...s.myBet, status: 'CashedOut' as const, multiplier: e.multiplier, payout: e.payout }
                  : s.myBet
            };
          });
        },

        balanceUpdated: (e: BalanceUpdatedEvent) => wallet.setBalance(e.balance),

        onlineCount: (e: OnlineCountEvent) => patchState(store, { online: e.count }),

        reconnected: () => void loadCurrentRound()
      };

      /** (Re)connect to SignalR, e.g. after login, so the connection carries the new identity. */
      const connect = async (): Promise<void> => {
        try {
          await hub.connect(store.slug(), handlers);
        } catch {
          toasts.error('Live connection failed. The game will not update until you reconnect.');
        }
      };

      /**
       * Milliseconds since the round started, as precisely as possible:
       * last server tick + time since it arrived (capped), or the start time on the corrected server clock.
       */
      const elapsedMs = (perfNow: number, dateNow: number): number => {
        const round = store.round();
        const crashPoint = store.crashPoint();

        if (crashPoint !== null && (round?.state === 'Crashed' || round?.state === 'Finished')) {
          return elapsedFor(crashPoint);
        }
        if (round?.state !== 'Running') {
          return 0;
        }

        const tick = store.tick();
        if (tick) {
          return tick.elapsedMs + Math.min(perfNow - tick.receivedAt, MAX_EXTRAPOLATION_MS);
        }
        return round.startedAt ? Math.max(0, dateNow + store.clockOffsetMs() - Date.parse(round.startedAt)) : 0;
      };

      return {
        async init(slug: string): Promise<void> {
          patchState(store, { slug });

          const [game, history] = await Promise.all([
            firstValueFrom(gamesApi.getGame(slug)),
            firstValueFrom(gamesApi.roundHistory(slug, 1, HISTORY_SIZE)).catch(() => null)
          ]);

          patchState(store, {
            game,
            history: (history?.items ?? []).map((r) => ({
              roundId: r.id,
              roundNumber: r.roundNumber,
              crashPoint: r.crashPoint,
              cancelled: r.state === 'Cancelled'
            }))
          });

          await loadCurrentRound();
          patchState(store, { loaded: true });
          await connect();
        },

        connect,

        async destroy(): Promise<void> {
          await hub.disconnect();
        },

        async placeBet(amount: number, autoCashOutAt: number | null): Promise<void> {
          patchState(store, { placing: true });
          try {
            const result = await firstValueFrom(
              betsApi.place({ gameSlug: store.slug(), amount, autoCashOutAt, slot: 1 })
            );
            patchState(store, { myBet: toMyBet(result.bet) });
            wallet.setBalance(result.balance);
            reminder.recordBet(amount);
            toasts.success(`Bet placed: ${amount.toFixed(2)} VLX`);
          } catch {
            // Reason already shown by the error interceptor (betting closed, limit, balance...)
          } finally {
            patchState(store, { placing: false });
          }
        },

        async cashOut(): Promise<void> {
          const bet = store.myBet();
          if (!bet) {
            return;
          }

          patchState(store, { cashingOut: true });
          try {
            const result = await firstValueFrom(betsApi.cashOut(bet.betId));
            patchState(store, { myBet: toMyBet(result.bet) });
            wallet.setBalance(result.balance);
            toasts.success(
              `Cashed out at ${result.bet.cashOutMultiplier?.toFixed(2)}x: +${result.bet.payout?.toFixed(2)} VLX`
            );
          } catch {
            // e.g. "Too late: the round has already crashed"
          } finally {
            patchState(store, { cashingOut: false });
          }
        },

        elapsedMs,

        /** The number shown in big letters. After the crash it is exactly the crash point. */
        multiplier(perfNow: number, dateNow: number): number {
          const state = store.round()?.state;
          const crashPoint = store.crashPoint();
          if (crashPoint !== null && (state === 'Crashed' || state === 'Finished')) {
            return crashPoint;
          }
          return state === 'Running' ? multiplierAt(elapsedMs(perfNow, dateNow)) : 1;
        },

        /** Seconds until betting closes, on the corrected server clock. */
        bettingSecondsLeft(dateNow: number): number {
          const endsAt = store.round()?.bettingEndsAt;
          if (store.round()?.state !== 'BettingOpen' || !endsAt) {
            return 0;
          }
          return Math.max(0, (Date.parse(endsAt) - (dateNow + store.clockOffsetMs())) / 1000);
        },

        /** Exposed for unit tests: feed events without a real SignalR connection. */
        eventHandlers: (): CrashHubHandlers => handlers
      };
    }
  )
);
