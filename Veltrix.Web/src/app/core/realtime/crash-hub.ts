import { Injectable, InjectionToken, inject, signal } from '@angular/core';
// Type only: the SignalR client itself is loaded on first connect (see HUB_CONNECTION_FACTORY)
import type { HubConnection } from '@microsoft/signalr';
import { AuthStore } from '../auth/auth.store';
import {
  BalanceUpdatedEvent,
  BetPlacedEvent,
  MultiplierTickEvent,
  NotificationDto,
  OnlineCountEvent,
  PlayerCashedOutEvent,
  RoundCrashedEvent,
  RoundStateChangedEvent
} from '../models/api.models';

export type HubStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/** Builds a (not yet started) connection; replaced by a fake in unit tests. */
export type HubConnectionFactory = (accessToken: () => Promise<string>) => Promise<HubConnection>;

export const HUB_CONNECTION_FACTORY = new InjectionToken<HubConnectionFactory>('HUB_CONNECTION_FACTORY', {
  providedIn: 'root',
  factory: () => async (accessToken) => {
    // Loaded on demand: the SignalR client is not part of the first download of the page
    const { HubConnectionBuilder, LogLevel } = await import('@microsoft/signalr');
    return (
      new HubConnectionBuilder()
        // WebSockets cannot send headers, so the token goes in the query string (?access_token=)
        .withUrl('/hubs/crash', { accessTokenFactory: accessToken })
        // Retry after 0, 2, 5, 10, 20 and 30 seconds, then give up (the game page offers a manual retry)
        .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 30000])
        .configureLogging(LogLevel.Warning)
        .build()
    );
  }
});

/** What a game page (Crash page, operator monitor) wants to happen for each game event. */
export interface CrashHubHandlers {
  roundStateChanged(event: RoundStateChangedEvent): void;
  multiplierTick(event: MultiplierTickEvent): void;
  roundCrashed(event: RoundCrashedEvent): void;
  betPlaced(event: BetPlacedEvent): void;
  playerCashedOut(event: PlayerCashedOutEvent): void;
  balanceUpdated(event: BalanceUpdatedEvent): void;
  onlineCount(event: OnlineCountEvent): void;
  /** After an automatic reconnect: events may have been missed, so reload the current state. */
  reconnected(): void;
}

/** Personal events of the logged-in user, wanted on EVERY page (not only the game). */
export interface UserHubHandlers {
  balanceUpdated(event: BalanceUpdatedEvent): void;
  notification(event: NotificationDto): void;
  reconnected(): void;
}

/**
 * ONE SignalR connection to /hubs/crash per browser tab, shared by:
 * - the logged-in user (balance and bell notifications on every page), and
 * - a game page, which additionally joins the game's group to receive round events.
 * One connection instead of two means the server's "online" counter counts each tab once.
 *
 * It only RECEIVES events; bets and cash-outs go through the REST API
 * (validation, rate limiting and ProblemDetails apply to them).
 */
@Injectable({ providedIn: 'root' })
export class CrashHub {
  private readonly auth = inject(AuthStore);
  private readonly createConnection = inject(HUB_CONNECTION_FACTORY);

  readonly status = signal<HubStatus>('disconnected');

  private connection: HubConnection | null = null;
  /** The user id the open connection was authenticated with (null = guest). */
  private connectedAs: number | null = null;
  private joinedSlug: string | null = null;
  /**
   * The server sends OnlineCount when a connection opens, often before a game page is listening
   * (the logged-in connection starts first). Kept here and handed to the page when it joins.
   */
  private lastOnline: OnlineCountEvent | null = null;

  private game: { slug: string; handlers: CrashHubHandlers } | null = null;
  private user: UserHubHandlers | null = null;

  /** Start / stop / join run strictly one after another, so overlapping calls can never interleave. */
  private queue: Promise<void> = Promise.resolve();

  /** A game page: receive the events of one game. Rejects if the connection cannot be made. */
  connect(slug: string, handlers: CrashHubHandlers): Promise<void> {
    this.game = { slug, handlers };
    return this.sync(true);
  }

  /** The game page was left. The connection stays open while a user is logged in. */
  disconnect(): Promise<void> {
    this.game = null;
    return this.sync();
  }

  /** Logged in: personal events on every page. Also used after login, so the connection carries the new identity. */
  startUserSession(handlers: UserHubHandlers): Promise<void> {
    this.user = handlers;
    return this.sync();
  }

  endUserSession(): Promise<void> {
    this.user = null;
    return this.sync();
  }

  private sync(rejectOnError = false): Promise<void> {
    const run = this.queue.then(() => this.apply());
    this.queue = run.catch(() => undefined);
    return rejectOnError ? run : run.catch(() => undefined);
  }

  /** Makes the real connection match what is wanted right now. */
  private async apply(): Promise<void> {
    const wanted = this.game !== null || this.user !== null;
    const identity = this.auth.user()?.id ?? null;

    // Logged in or out since the connection was opened: the server must see the new identity
    if (this.connection && (!wanted || this.connectedAs !== identity)) {
      await this.stop();
    }
    if (!wanted) {
      return;
    }
    if (!this.connection) {
      await this.start(identity);
    }

    const connection = this.connection!;
    const slug = this.game?.slug ?? null;
    if (this.joinedSlug && this.joinedSlug !== slug) {
      await connection.invoke('LeaveGame', this.joinedSlug).catch(() => undefined);
      this.joinedSlug = null;
    }
    if (slug && this.joinedSlug !== slug) {
      await connection.invoke('JoinGame', slug);
      this.joinedSlug = slug;
      if (this.lastOnline) {
        this.game?.handlers.onlineCount(this.lastOnline);
      }
    }
  }

  private async start(identity: number | null): Promise<void> {
    const connection = await this.createConnection(async () => (await this.auth.getValidAccessToken()) ?? '');

    // Handlers are looked up at event time, so a page can come and go without a new connection
    connection.on('RoundStateChanged', (e: RoundStateChangedEvent) => this.game?.handlers.roundStateChanged(e));
    connection.on('MultiplierTick', (e: MultiplierTickEvent) => this.game?.handlers.multiplierTick(e));
    connection.on('RoundCrashed', (e: RoundCrashedEvent) => this.game?.handlers.roundCrashed(e));
    connection.on('BetPlaced', (e: BetPlacedEvent) => this.game?.handlers.betPlaced(e));
    connection.on('PlayerCashedOut', (e: PlayerCashedOutEvent) => this.game?.handlers.playerCashedOut(e));
    connection.on('OnlineCount', (e: OnlineCountEvent) => {
      this.lastOnline = e;
      this.game?.handlers.onlineCount(e);
    });
    connection.on('BalanceUpdated', (e: BalanceUpdatedEvent) => (this.user ?? this.game?.handlers)?.balanceUpdated(e));
    connection.on('Notification', (e: NotificationDto) => this.user?.notification(e));

    connection.onreconnecting(() => this.status.set('reconnecting'));
    connection.onreconnected(async () => {
      this.status.set('connected');
      // Group membership is lost when the connection drops; join again
      if (this.joinedSlug) {
        await connection.invoke('JoinGame', this.joinedSlug).catch(() => undefined);
      }
      this.game?.handlers.reconnected();
      this.user?.reconnected();
    });
    connection.onclose(() => {
      // Only if it closed by itself (reconnect gave up); a deliberate stop() already cleared it
      if (this.connection === connection) {
        this.connection = null;
        this.joinedSlug = null;
        this.status.set('disconnected');
      }
    });

    this.connection = connection;
    this.connectedAs = identity;
    this.status.set('connecting');

    try {
      await connection.start();
      this.status.set('connected');
    } catch (error) {
      this.connection = null;
      this.status.set('disconnected');
      throw error;
    }
  }

  private async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.joinedSlug = null;
    this.lastOnline = null;
    this.status.set('disconnected');
    await connection?.stop().catch(() => undefined);
  }
}
