import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  untracked
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { CrashHub } from '../../core/realtime/crash-hub';
import { WalletStore } from '../../core/state/wallet.store';
import { MultiplierPipe, ShortHashPipe } from '../../shared/pipes';
import { BetPanel, BetRequest } from './components/bet-panel';
import { CrashChart } from './components/crash-chart';
import { HistoryStrip } from './components/history-strip';
import { LiveBets } from './components/live-bets';
import { CrashStore } from './crash.store';

@Component({
  selector: 'vx-crash-page',
  imports: [RouterLink, CrashChart, BetPanel, LiveBets, HistoryStrip, MultiplierPipe, ShortHashPipe],
  // A fresh store for every visit; it is destroyed (and SignalR disconnected) when the user leaves the page
  providers: [CrashStore],
  templateUrl: './crash-page.html',
  styleUrl: './crash-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CrashPage {
  protected readonly store = inject(CrashStore);
  protected readonly auth = inject(AuthStore);
  protected readonly wallet = inject(WalletStore);
  protected readonly hub = inject(CrashHub);

  /**
   * Animation clock, updated on every screen refresh (requestAnimationFrame).
   * Everything time-based below is a computed() of it, so the view only changes when a value really changes.
   */
  private readonly frame = signal({ perf: performance.now(), date: Date.now() });

  protected readonly elapsedMs = computed(() => this.store.elapsedMs(this.frame().perf, this.frame().date));
  protected readonly multiplier = computed(() => this.store.multiplier(this.frame().perf, this.frame().date));
  protected readonly secondsLeft = computed(() => Math.ceil(this.store.bettingSecondsLeft(this.frame().date) * 10) / 10);

  protected readonly bettingProgress = computed(() => {
    const total = this.store.game()?.bettingDurationSec ?? 10;
    return Math.min(100, Math.max(0, (this.secondsLeft() / total) * 100));
  });

  protected readonly flying = computed(() => {
    const state = this.store.state();
    return state === 'Running' || state === 'Crashed' || state === 'Finished';
  });

  protected readonly crashed = computed(() => {
    const state = this.store.state();
    return state === 'Crashed' || state === 'Finished';
  });

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      let frameId = 0;
      const loop = () => {
        this.frame.set({ perf: performance.now(), date: Date.now() });
        frameId = requestAnimationFrame(loop);
      };
      frameId = requestAnimationFrame(loop);
      destroyRef.onDestroy(() => cancelAnimationFrame(frameId));
    });

    void this.store.init('crash');
    destroyRef.onDestroy(() => void this.store.destroy());

    // Log in / log out while on this page: reconnect, so the server knows who this connection belongs to
    const userId = computed(() => this.auth.user()?.id ?? null);
    let firstRun = true;
    effect(() => {
      userId();
      untracked(() => {
        if (!firstRun && this.store.loaded()) {
          void this.store.connect();
        }
        firstRun = false;
      });
    });
  }

  protected placeBet(request: BetRequest): void {
    void this.store.placeBet(request.amount, request.autoCashOutAt);
  }

  protected cashOut(): void {
    void this.store.cashOut();
  }

  protected reconnect(): void {
    void this.store.connect();
  }
}
