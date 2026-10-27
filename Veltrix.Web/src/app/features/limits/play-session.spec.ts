import { BREAK_MS, PlaySession, isReminderDue, minutesPlayed, withBet, withReminderShown } from './play-session';

const MIN = 60_000;

describe('play session (session reminder)', () => {
  it('the first bet starts a session', () => {
    const session = withBet(null, 10, 1_000);
    expect(session).toEqual({ startedAt: 1_000, lastActivityAt: 1_000, lastReminderAt: null, bets: 1, wagered: 10 });
  });

  it('bets close together continue the same session', () => {
    let session = withBet(null, 10, 0);
    session = withBet(session, 5, 10 * MIN);
    expect(session.startedAt).toBe(0);
    expect(session.bets).toBe(2);
    expect(session.wagered).toBe(15);
  });

  it('a break longer than 15 minutes starts a new session', () => {
    let session = withBet(null, 10, 0);
    session = withBet(session, 5, BREAK_MS + 1);
    expect(session.startedAt).toBe(BREAK_MS + 1);
    expect(session.bets).toBe(1);
  });

  it('reminds after every interval of continuous play', () => {
    // A bet every 10 minutes for over two hours, checking like the app's timer does
    let session: PlaySession | null = null;
    const remindedAt: number[] = [];
    for (let t = 0; t <= 130 * MIN; t += 10 * MIN) {
      session = withBet(session, 1, t);
      if (isReminderDue(session, 60, t)) {
        remindedAt.push(t / MIN);
        session = withReminderShown(session, t);
      }
    }
    expect(remindedAt).toEqual([60, 120]);
    expect(minutesPlayed(session!, 130 * MIN)).toBe(130);
    expect(session!.bets).toBe(14);
  });

  it('never reminds when switched off, without a session, or after a break', () => {
    const session = withBet(null, 1, 0);
    expect(isReminderDue(session, 0, 120 * MIN)).toBeFalse();
    expect(isReminderDue(null, 60, 120 * MIN)).toBeFalse();
    // The last bet was 2 hours ago: the player is not playing right now
    expect(isReminderDue(session, 60, 120 * MIN)).toBeFalse();
  });
});
