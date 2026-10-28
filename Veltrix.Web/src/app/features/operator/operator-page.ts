import { ChangeDetectionStrategy, Component, DestroyRef, afterNextRender, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { I18n, mark } from '../../core/i18n/i18n';
import { LocalDatePipe, TranslateLabelsPipe, TranslatePipe, TranslateSplitPipe } from '../../core/i18n/i18n-pipes';
import { GameDto, GameStatus, RoundCancellationDto, RoundState } from '../../core/models/api.models';
import { CrashHub } from '../../core/realtime/crash-hub';
import { ToastStore } from '../../core/ui/toast.store';
import { GAME_STATUS_LABELS, ROUND_STATE_LABELS } from '../../shared/labels';
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

const STATUS_TONES: Record<GameStatus, BadgeTone> = { Active: 'success', Maintenance: 'warning', Disabled: 'neutral' };

/**
 * Operator panel: the live round (the same real-time store as the Crash page, read-only here),
 * cancelling it with automatic refunds, and the games' settings and status.
 */
@Component({
  selector: 'vx-operator-page',
  imports: [
    LocalDatePipe,
    TranslatePipe,
    TranslateLabelsPipe,
    TranslateSplitPipe,
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
  private readonly i18n = inject(I18n);

  protected readonly icons = Icons;
  protected readonly stateLabels = ROUND_STATE_LABELS;
  protected readonly statusLabels = GAME_STATUS_LABELS;
  protected readonly tabs: TabItem[] = [
    { value: 'live', label: mark('Live round') },
    { value: 'games', label: mark('Games') }
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
      ariaLabel: this.i18n.t('Cancel round'),
      width: '480px'
    });
    const result = await firstValueFrom(ref.closed);
    if (result) {
      this.toasts.success(
        this.i18n.t('Round #{number} cancelled: {count} bets refunded ({amount} VLX).', {
          number: round.roundNumber,
          count: result.refundedBetsCount,
          amount: result.refundedTotal.toFixed(2)
        })
      );
    }
  }

  protected async editGame(game: GameDto | null): Promise<void> {
    const ref = this.modal.open<GameDto, GameDto | null, GameFormDialog>(GameFormDialog, {
      data: game,
      ariaLabel: game ? this.i18n.t('Edit {name}', { name: game.name }) : this.i18n.t('New game'),
      width: '560px'
    });
    const saved = await firstValueFrom(ref.closed);
    if (saved) {
      this.toasts.success(
        game ? this.i18n.t('{name} saved.', { name: saved.name }) : this.i18n.t('{name} created.', { name: saved.name })
      );
      await this.loadGames();
    }
  }

  protected async changeStatus(game: GameDto, status: GameStatus): Promise<void> {
    if (status === game.status) {
      return;
    }
    if (status !== 'Active') {
      const statusName = this.i18n.t(GAME_STATUS_LABELS[status]);
      const confirmed = await this.modal.confirm({
        title: this.i18n.t('Set {name} to {status}?', { name: game.name, status: statusName }),
        message:
          status === 'Disabled'
            ? this.i18n.t('Players will no longer see the game, and no new bets are accepted.')
            : this.i18n.t('No new bets are accepted until the game is Active again. A round already running finishes normally.'),
        confirmText: this.i18n.t('Set to {status}', { status: statusName }),
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
      this.toasts.success(this.i18n.t('{name} is now {status}.', { name: updated.name, status: this.i18n.t(GAME_STATUS_LABELS[updated.status]) }));
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
