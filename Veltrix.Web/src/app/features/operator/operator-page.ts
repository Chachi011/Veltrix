import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, afterNextRender, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { GameDto, GameStatus, RoundCancellationDto, RoundState } from '../../core/models/api.models';
import { CrashHub } from '../../core/realtime/crash-hub';
import { ToastStore } from '../../core/ui/toast.store';
import { MultiplierPipe, VlxPipe } from '../../shared/pipes';
import {
  Badge,
  BadgeTone,
  Button,
  Card,
  EmptyState,
  HashBadge,
  Icons,
  ModalService,
  MultiplierPill,
  PageHeader,
  Skeleton,
  StatCard,
  TabItem,
  Tabs
} from '../../shared/ui';
import { LiveBets } from '../crash/components/live-bets';
import { CrashStore } from '../crash/crash.store';
import { CancelRoundData, CancelRoundDialog } from './cancel-round-dialog';
import { GameFormDialog } from './game-form-dialog';

const STATE_TONES: Record<RoundState, BadgeTone> = {
  Waiting: 'neutral',
  BettingOpen: 'primary',
  BettingClosed: 'warning',
  Running: 'accent',
  Crashed: 'danger',
  Finished: 'neutral',
  Cancelled: 'warning'
};

const STATE_LABELS: Record<RoundState, string> = {
  Waiting: 'Waiting',
  BettingOpen: 'Betting open',
  BettingClosed: 'Betting closed',
  Running: 'Running',
  Crashed: 'Crashed',
  Finished: 'Finished',
  Cancelled: 'Cancelled'
};

const STATUS_TONES: Record<GameStatus, BadgeTone> = { Active: 'success', Maintenance: 'warning', Disabled: 'neutral' };

/**
 * Operator panel: the live round (the same real-time store as the Crash page, read-only here),
 * cancelling it with automatic refunds, and the games' settings and status.
 */
@Component({
  selector: 'vx-operator-page',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    LucideAngularModule,
    MultiplierPipe,
    VlxPipe,
    Badge,
    Button,
    Card,
    EmptyState,
    HashBadge,
    LiveBets,
    MultiplierPill,
    PageHeader,
    Skeleton,
    StatCard,
    Tabs
  ],
  providers: [CrashStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './operator-page.html'
})
export class OperatorPage {
  protected readonly store = inject(CrashStore);
  protected readonly hub = inject(CrashHub);
  private readonly gamesApi = inject(GamesApi);
  private readonly modal = inject(ModalService);
  private readonly toasts = inject(ToastStore);

  protected readonly icons = Icons;
  protected readonly tabs: TabItem[] = [
    { value: 'live', label: 'Live round' },
    { value: 'games', label: 'Games' }
  ];
  protected readonly tab = signal('live');
  protected readonly statuses: GameStatus[] = ['Active', 'Maintenance', 'Disabled'];

  protected readonly games = signal<GameDto[] | null>(null);
  protected readonly changingStatus = signal<number | null>(null);

  /** Animation clock (requestAnimationFrame), like the Crash page, for the live multiplier. */
  private readonly frame = signal({ perf: performance.now(), date: Date.now() });
  protected readonly multiplier = computed(() => this.store.multiplier(this.frame().perf, this.frame().date));
  protected readonly secondsLeft = computed(() => Math.ceil(this.store.bettingSecondsLeft(this.frame().date)));

  protected readonly activeBets = computed(() => this.store.liveBets().filter((b) => b.status === 'Active'));
  protected readonly activeTotal = computed(() => this.activeBets().reduce((sum, b) => sum + b.amount, 0));
  protected readonly cashedOutTotal = computed(() =>
    this.store.liveBets().reduce((sum, b) => sum + (b.status === 'CashedOut' ? (b.payout ?? 0) : 0), 0)
  );
  /** What the active bets would pay if everyone cashed out right now. */
  protected readonly exposure = computed(() => this.activeTotal() * (this.store.state() === 'Running' ? this.multiplier() : 1));

  protected readonly canCancel = computed(() => {
    const state = this.store.state();
    return state !== null && state !== 'Crashed' && state !== 'Finished' && state !== 'Cancelled';
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
    void this.loadGames();
  }

  protected stateTone(state: RoundState | null): BadgeTone {
    return state ? STATE_TONES[state] : 'neutral';
  }

  protected stateLabel(state: RoundState | null): string {
    return state ? STATE_LABELS[state] : 'No round yet';
  }

  protected statusTone(status: GameStatus): BadgeTone {
    return STATUS_TONES[status];
  }

  protected async cancelRound(): Promise<void> {
    const round = this.store.round();
    if (!round) {
      return;
    }
    const ref = this.modal.open<RoundCancellationDto, CancelRoundData, CancelRoundDialog>(CancelRoundDialog, {
      data: {
        roundId: round.id,
        roundNumber: round.roundNumber,
        activeBets: this.activeBets().length,
        activeTotal: this.activeTotal()
      },
      ariaLabel: 'Cancel round',
      width: '480px'
    });
    const result = await firstValueFrom(ref.closed);
    if (result) {
      this.toasts.success(
        `Round #${round.roundNumber} cancelled: ${result.refundedBetsCount} bets refunded (${result.refundedTotal.toFixed(2)} VLX).`
      );
    }
  }

  protected async editGame(game: GameDto | null): Promise<void> {
    const ref = this.modal.open<GameDto, GameDto | null, GameFormDialog>(GameFormDialog, {
      data: game,
      ariaLabel: game ? `Edit ${game.name}` : 'New game',
      width: '560px'
    });
    const saved = await firstValueFrom(ref.closed);
    if (saved) {
      this.toasts.success(game ? `${saved.name} saved.` : `${saved.name} created.`);
      await this.loadGames();
    }
  }

  protected async changeStatus(game: GameDto, status: GameStatus): Promise<void> {
    if (status === game.status) {
      return;
    }
    if (status !== 'Active') {
      const confirmed = await this.modal.confirm({
        title: `Set ${game.name} to ${status}?`,
        message:
          status === 'Disabled'
            ? 'Players will no longer see the game, and no new bets are accepted.'
            : 'No new bets are accepted until the game is Active again. A round already running finishes normally.',
        confirmText: `Set to ${status}`,
        tone: 'danger'
      });
      if (!confirmed) {
        await this.loadGames(); // puts the drop-down back
        return;
      }
    }

    this.changingStatus.set(game.id);
    try {
      const updated = await firstValueFrom(this.gamesApi.setStatus(game.id, status));
      this.games.update((list) => list?.map((g) => (g.id === updated.id ? updated : g)) ?? null);
      this.toasts.success(`${updated.name} is now ${updated.status}.`);
    } catch {
      await this.loadGames();
    } finally {
      this.changingStatus.set(null);
    }
  }

  private async loadGames(): Promise<void> {
    this.games.set(await firstValueFrom(this.gamesApi.list()).catch(() => []));
  }
}
