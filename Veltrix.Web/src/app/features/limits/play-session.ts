// "Continuous play" for the session reminder, as pure functions (unit-tested without timers or a browser).

/** A gap longer than this between two bets ends the session: the player took a break. */
export const BREAK_MS = 15 * 60_000;

export interface PlaySession {
  startedAt: number;
  lastActivityAt: number;
  /** When the last reminder was shown (null = none yet in this session). */
  lastReminderAt: number | null;
  bets: number;
  wagered: number;
}

/** A bet was placed: continue the session, or start a new one after a break. */
export function withBet(session: PlaySession | null, amount: number, now: number): PlaySession {
  if (!session || now - session.lastActivityAt > BREAK_MS) {
    return { startedAt: now, lastActivityAt: now, lastReminderAt: null, bets: 1, wagered: amount };
  }
  return { ...session, lastActivityAt: now, bets: session.bets + 1, wagered: session.wagered + amount };
}

/** True when `intervalMin` minutes of continuous play have passed since the start or the last reminder. */
export function isReminderDue(session: PlaySession | null, intervalMin: number, now: number): boolean {
  if (!session || intervalMin <= 0 || now - session.lastActivityAt > BREAK_MS) {
    return false;
  }
  return now - (session.lastReminderAt ?? session.startedAt) >= intervalMin * 60_000;
}

export function withReminderShown(session: PlaySession, now: number): PlaySession {
  return { ...session, lastReminderAt: now };
}

export function minutesPlayed(session: PlaySession, now: number): number {
  return Math.floor((now - session.startedAt) / 60_000);
}
