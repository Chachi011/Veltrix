import { ChangeDetectionStrategy, Component, DestroyRef, afterNextRender, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { RoundState } from '../../core/models/api.models';
import { MultiplierPipe, VlxPipe } from '../../shared/pipes';
import { Badge, BadgeTone, Button, Card, Icons, Skeleton } from '../../shared/ui';
import { HistoryStrip } from '../crash/components/history-strip';
import { CrashStore } from '../crash/crash.store';

const STATE: Record<RoundState, { label: string; tone: BadgeTone }> = {
  Waiting: { label: 'Next round soon', tone: 'neutral' },
  BettingOpen: { label: 'Betting open', tone: 'primary' },
  BettingClosed: { label: 'Starting', tone: 'warning' },
  Running: { label: 'Flying', tone: 'accent' },
  Crashed: { label: 'Crashed', tone: 'danger' },
  Finished: { label: 'Crashed', tone: 'danger' },
  Cancelled: { label: 'Cancelled', tone: 'warning' }
};

/**
 * The live round on the home page: the same real-time store as the Crash page (its own instance),
 * so guests see the game moving before they even open it.
 */
@Component({
  selector: 'vx-live-round-card',
  imports: [RouterLink, LucideAngularModule, MultiplierPipe, VlxPipe, Badge, Button, Card, HistoryStrip, Skeleton],
  providers: [CrashStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  // A grid child with a wide history strip inside: min-w-0 lets it shrink (the strip scrolls instead)
  host: { class: 'block min-w-0' },
  template: `
    <vx-card title="Live now" subtitle="The main Crash table, updated in real time.">
      @if (store.round(); as round) {
        <vx-badge card-actions [tone]="state().tone" dot>{{ state().label }}</vx-badge>
      }

      @if (store.loaded()) {
        <div class="flex flex-wrap items-end justify-between gap-4">
          <div>
            @if (store.state() === 'BettingOpen') {
              <p class="text-[13px] text-muted">Betting closes in</p>
              <p class="num text-5xl font-semibold tracking-tight">{{ secondsLeft() }}s</p>
            } @else if (store.state() === 'Running') {
              <p class="text-[13px] text-muted">Round #{{ store.round()?.roundNumber }}</p>
              <p class="num text-5xl font-semibold tracking-tight" aria-hidden="true">{{ multiplier() | mult }}</p>
            } @else if (store.crashPoint() !== null) {
              <p class="text-[13px] text-muted">Crashed at</p>
              <p class="num text-5xl font-semibold tracking-tight text-danger">{{ store.crashPoint() | mult }}</p>
            } @else {
              <p class="text-[13px] text-muted">Round #{{ store.round()?.roundNumber ?? '—' }}</p>
              <p class="text-3xl font-semibold">Starting soon</p>
            }
          </div>
          <dl class="flex gap-6 text-sm">
            <div>
              <dt class="text-xs text-muted">Bets</dt>
              <dd class="num font-semibold">{{ store.liveBets().length }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted">Staked</dt>
              <dd class="num font-semibold">{{ store.totalBet() | vlx: false }}</dd>
            </div>
            <div>
              <dt class="text-xs text-muted">Online</dt>
              <dd class="num font-semibold">{{ store.online() }}</dd>
            </div>
          </dl>
        </div>
        <vx-history-strip class="mt-5" [items]="store.history()" />
        <a vxButton routerLink="/crash" variant="primary" class="mt-5" block>
          <lucide-icon [img]="icons.rocket" [size]="16" aria-hidden="true" />Open the game
        </a>
      } @else {
        <div class="flex flex-col gap-3">
          <vx-skeleton width="45%" height="3rem" />
          <vx-skeleton height="2rem" />
          <vx-skeleton height="2.5rem" />
        </div>
      }
    </vx-card>
  `
})
export class LiveRoundCard {
  protected readonly store = inject(CrashStore);
  protected readonly icons = Icons;

  private readonly frame = signal({ perf: performance.now(), date: Date.now() });
  protected readonly multiplier = computed(() => this.store.multiplier(this.frame().perf, this.frame().date));
  protected readonly secondsLeft = computed(() => Math.ceil(this.store.bettingSecondsLeft(this.frame().date)));
  protected readonly state = computed(() => STATE[this.store.state() ?? 'Waiting']);

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

    this.store.init('crash').catch(() => undefined);
    destroyRef.onDestroy(() => void this.store.destroy());
  }
}
