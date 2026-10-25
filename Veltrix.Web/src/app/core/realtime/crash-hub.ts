import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthStore } from '../auth/auth.store';
import {
  BalanceUpdatedEvent,
  BetPlacedEvent,
  MultiplierTickEvent,
  OnlineCountEvent,
  PlayerCashedOutEvent,
  RoundCrashedEvent,
  RoundStateChangedEvent
} from '../models/api.models';

export type HubStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/** What the page wants to happen for each server event. */
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

/**
 * The SignalR connection to /hubs/crash. It only RECEIVES game events; bets and cash-outs go
 * through the REST API (validation, rate limiting and ProblemDetails apply to them).
 */
@Injectable({ providedIn: 'root' })
export class CrashHub {
  private readonly auth = inject(AuthStore);
  private connection: HubConnection | null = null;

  /** Increases on every connect(); stops an old, slower connect() from overwriting a newer one. */
  private generation = 0;

  readonly status = signal<HubStatus>('disconnected');

  async connect(slug: string, handlers: CrashHubHandlers): Promise<void> {
    await this.disconnect();
    const generation = ++this.generation;

    const connection = new HubConnectionBuilder()
      // WebSockets cannot send headers, so the token goes in the query string (?access_token=)
      .withUrl('/hubs/crash', { accessTokenFactory: async () => (await this.auth.getValidAccessToken()) ?? '' })
      // Retry after 0, 2, 5, 10, 20 and 30 seconds, then give up (the page offers a manual retry)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('RoundStateChanged', handlers.roundStateChanged);
    connection.on('MultiplierTick', handlers.multiplierTick);
    connection.on('RoundCrashed', handlers.roundCrashed);
    connection.on('BetPlaced', handlers.betPlaced);
    connection.on('PlayerCashedOut', handlers.playerCashedOut);
    connection.on('BalanceUpdated', handlers.balanceUpdated);
    connection.on('OnlineCount', handlers.onlineCount);

    connection.onreconnecting(() => this.status.set('reconnecting'));
    connection.onreconnected(async () => {
      this.status.set('connected');
      // Group membership is lost when the connection drops; join again
      await connection.invoke('JoinGame', slug);
      handlers.reconnected();
    });
    connection.onclose(() => {
      if (generation === this.generation) {
        this.status.set('disconnected');
      }
    });

    this.connection = connection;
    this.status.set('connecting');

    try {
      await connection.start();
      if (generation !== this.generation) {
        await connection.stop();
        return;
      }
      await connection.invoke('JoinGame', slug);
      this.status.set('connected');
    } catch (error) {
      if (generation === this.generation) {
        this.status.set('disconnected');
      }
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    this.generation++;
    const connection = this.connection;
    this.connection = null;
    if (connection) {
      await connection.stop().catch(() => undefined);
    }
    this.status.set('disconnected');
  }
}
