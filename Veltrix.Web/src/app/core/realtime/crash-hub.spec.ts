import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { HubConnection } from '@microsoft/signalr';
import { AuthStore } from '../auth/auth.store';
import { NotificationDto, UserDto } from '../models/api.models';
import { CrashHub, CrashHubHandlers, HUB_CONNECTION_FACTORY, UserHubHandlers } from './crash-hub';

/** Just enough of a SignalR connection to watch what the hub does with it. */
class FakeConnection {
  readonly handlers = new Map<string, (event: unknown) => void>();
  readonly invoked: [string, unknown][] = [];
  started = false;
  stopped = false;

  on(name: string, handler: (event: unknown) => void): void {
    this.handlers.set(name, handler);
  }
  onreconnecting(): void {}
  onreconnected(): void {}
  onclose(): void {}
  async start(): Promise<void> {
    this.started = true;
  }
  async stop(): Promise<void> {
    this.stopped = true;
  }
  async invoke(name: string, arg: unknown): Promise<void> {
    this.invoked.push([name, arg]);
  }
  /** The server sends an event. */
  emit(name: string, event: unknown): void {
    this.handlers.get(name)?.(event);
  }
}

const gameHandlers = (): jasmine.SpyObj<CrashHubHandlers> =>
  jasmine.createSpyObj('game', [
    'roundStateChanged',
    'multiplierTick',
    'roundCrashed',
    'betPlaced',
    'playerCashedOut',
    'balanceUpdated',
    'onlineCount',
    'reconnected'
  ]);

const userHandlers = (): jasmine.SpyObj<UserHubHandlers> => jasmine.createSpyObj('user', ['balanceUpdated', 'notification', 'reconnected']);

describe('CrashHub (one shared SignalR connection per tab)', () => {
  let hub: CrashHub;
  let connections: FakeConnection[];
  const user = signal<UserDto | null>(null);

  beforeEach(() => {
    connections = [];
    user.set({ id: 1, email: 'p@x.dev', nickname: 'p', role: 'Player', avatarUrl: null });

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthStore, useValue: { user, getValidAccessToken: async () => 'token' } },
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: async () => {
            const connection = new FakeConnection();
            connections.push(connection);
            return connection as unknown as HubConnection;
          }
        }
      ]
    });
    hub = TestBed.inject(CrashHub);
  });

  it('the logged-in session and the game page share ONE connection (the online counter counts the tab once)', async () => {
    await hub.startUserSession(userHandlers());
    await hub.connect('crash', gameHandlers());

    expect(connections.length).toBe(1);
    expect(connections[0].invoked).toEqual([['JoinGame', 'crash']]);
    expect(hub.status()).toBe('connected');
  });

  // Bug found while testing Phase 4: the Crash page showed "0 online", because the server's count
  // arrived when the user's connection opened, before the game page was listening
  it('gives a game page that joins later the last online count', async () => {
    await hub.startUserSession(userHandlers());
    connections[0].emit('OnlineCount', { count: 3 });

    const game = gameHandlers();
    await hub.connect('crash', game);

    expect(game.onlineCount).toHaveBeenCalledWith({ count: 3 });
  });

  it('leaving the game keeps the connection while logged in; logging out closes it', async () => {
    await hub.startUserSession(userHandlers());
    await hub.connect('crash', gameHandlers());

    await hub.disconnect();
    expect(connections[0].invoked).toContain(['LeaveGame', 'crash']);
    expect(connections[0].stopped).toBeFalse();

    await hub.endUserSession();
    expect(connections[0].stopped).toBeTrue();
    expect(hub.status()).toBe('disconnected');
  });

  it('a guest watching the game gets a new connection after logging in (the server must see the new identity)', async () => {
    user.set(null);
    await hub.connect('crash', gameHandlers());

    user.set({ id: 7, email: 'n@x.dev', nickname: 'n', role: 'Player', avatarUrl: null });
    await hub.startUserSession(userHandlers());

    expect(connections.length).toBe(2);
    expect(connections[0].stopped).toBeTrue();
    expect(connections[1].invoked).toEqual([['JoinGame', 'crash']]);
  });

  it('personal events go to the user session, game events to the game page', async () => {
    const personal = userHandlers();
    const game = gameHandlers();
    await hub.startUserSession(personal);
    await hub.connect('crash', game);

    const notification: NotificationDto = { id: 1, type: 'DailyBonus', title: 't', message: 'm', isRead: false, createdAt: '' };
    connections[0].emit('Notification', notification);
    connections[0].emit('BalanceUpdated', { balance: 50 });
    connections[0].emit('BetPlaced', { roundId: 1, nickname: 'a', amount: 5 });

    expect(personal.notification).toHaveBeenCalledWith(notification);
    expect(personal.balanceUpdated).toHaveBeenCalledWith({ balance: 50 });
    expect(game.betPlaced).toHaveBeenCalledWith({ roundId: 1, nickname: 'a', amount: 5 });
  });
});
