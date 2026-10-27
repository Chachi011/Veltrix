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
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../../core/auth/auth.store';
import { CrashHub } from '../../core/realtime/crash-hub';
import { LimitsStore } from '../../core/state/limits.store';
import { WalletStore } from '../../core/state/wallet.store';
import { MultiplierPipe } from '../../shared/pipes';
import { Badge, Button, Card, EmptyState, HashBadge, Icons, PageHeader, ProgressRing, Skeleton } from '../../shared/ui';
import { BetPanel, BetRequest } from './components/bet-panel';
import { CrashChart } from './components/crash-chart';
import { HistoryStrip } from './components/history-strip';
import { LiveBets } from './components/live-bets';
import { CrashStore } from './crash.store';

@Component({
  selector: 'vx-crash-page',
  imports: [
    RouterLink,
    LucideAngularModule,
    CrashChart,
    BetPanel,
    LiveBets,
    HistoryStrip,
    MultiplierPipe,
    Badge,
    Button,
    Card,
    EmptyState,
    HashBadge,
    PageHeader,
    ProgressRing,
    Skeleton
  ],
  // A fresh store for every visit; it is destroyed (and SignalR disconnected) when the user leaves the page
  providers: [CrashStore],
  templateUrl: './crash-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CrashPage {
  protected readonly store = inject(CrashStore);
  protected readonly auth = inject(AuthStore);
  protected readonly wallet = inject(WalletStore);
  protected readonly limits = inject(LimitsStore);
  protected readonly hub = inject(CrashHub);
  protected readonly icons = Icons;
  /** /crash plays the main game; /crash/:slug any other Crash game from the catalog. */
  protected readonly slug = inject(ActivatedRoute).snapshot.paramMap.get('slug') ?? 'crash';
  protected readonly notFound = signal(false);

  /**
   * Animation clock, updated on every screen refresh (requestAnimationFrame).
   * Everything time-based below is a computed() of it, so the view only changes when a value really changes.
   */
  private readonly frame = signal({ perf: performance.now(), date: Date.now() });

  protected readonly elapsedMs = computed(() => this.store.elapsedMs(this.frame().perf, this.frame().date));
  protected readonly multiplier = computed(() => this.store.multiplier(this.frame().perf, this.frame().date));
  protected readonly secondsLeft = computed(() => Math.ceil(this.store.bettingSecondsLeft(this.frame().date) * 10) / 10);

  /** 1 → 0 while betting is open (drives the countdown ring). */
  protected readonly bettingProgress = computed(() => {
    const total = this.store.game()?.bettingDurationSec ?? 10;
    return Math.min(1, Math.max(0, this.secondsLeft() / total));
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

    this.store.init(this.slug).catch(() => this.notFound.set(true)); // e.g. a mistyped or removed game
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
