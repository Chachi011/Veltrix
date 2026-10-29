import { DestroyRef, Injectable, Injector, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { LimitsStore } from '../../core/state/limits.store';
import { PlaySession, isReminderDue, minutesPlayed, withBet, withReminderShown } from './play-session';

const STORAGE_PREFIX = 'vx.playSession.';
const CHECK_EVERY_MS = 30_000;

/**
 * The session reminder (responsible play): every N minutes of continuous play (N = the player's
 * sessionReminderMin, default 60) a dialog shows how long they have played and how much they staked.
 * "Continuous" = bets without a break longer than 15 minutes. Kept per user in localStorage,
 * so reloading the page does not reset the clock.
 *
 * A reminder is advice, so it lives in the browser. The hard rules (daily limit, self-exclusion)
 * are enforced by the server. The dialog is loaded only when a reminder is due.
 */
@Injectable({ providedIn: 'root' })
export class SessionReminder {
  private readonly auth = inject(AuthStore);
  private readonly limits = inject(LimitsStore);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  private readonly session = signal<PlaySession | null>(null);
  private userId: number | null = null;
  private open = false;

  constructor() {
    const timer = setInterval(() => void this.check(), CHECK_EVERY_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  /** Called by the Crash page after a bet was accepted. */
  recordBet(amount: number): void {
    const userId = this.auth.user()?.id;
    if (userId === undefined) {
      return;
    }
    this.useUser(userId);
    this.save(withBet(this.session(), amount, Date.now()));
    void this.check();
  }

  private async check(): Promise<void> {
    const userId = this.auth.user()?.id;
    if (userId === undefined || !this.auth.isPlayer() || this.open) {
      return;
    }
    this.useUser(userId);

    const session = this.session();
    const now = Date.now();
    if (!session || !isReminderDue(session, this.limits.reminderMinutes(), now)) {
      return;
    }

    this.save(withReminderShown(session, now));
    this.open = true;
    try {
      const [{ SessionReminderDialog }, { ModalService }] = await Promise.all([
        import('./session-reminder-dialog'),
        import('../../shared/ui/modal')
      ]);
      const ref = this.injector.get(ModalService).open(SessionReminderDialog, {
        data: { minutes: minutesPlayed(session, now), bets: session.bets, wagered: session.wagered },
        ariaLabel: 'Session reminder'
      });
      if ((await firstValueFrom(ref.closed)) === 'limits') {
        await this.router.navigateByUrl('/limits');
      }
    } finally {
      this.open = false;
    }
  }

  /** Each user has their own session (two accounts on one computer do not share it). */
  private useUser(userId: number): void {
    if (this.userId === userId) {
      return;
    }
    this.userId = userId;
    this.session.set(read(userId));
  }

  private save(session: PlaySession): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_PREFIX + this.userId, JSON.stringify(session));
    } catch {
      // Storage blocked: the reminder still works until the page is reloaded
    }
  }
}

function read(userId: number): PlaySession | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + userId);
    return raw ? (JSON.parse(raw) as PlaySession) : null;
  } catch {
    return null;
  }
}
