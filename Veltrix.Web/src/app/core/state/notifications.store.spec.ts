import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { NotificationsApi } from '../api/notifications.api';
import { AuthStore } from '../auth/auth.store';
import { NotificationDto, NotificationType, UserRole } from '../models/api.models';
import { ToastStore } from '../ui/toast.store';
import { LimitsStore } from './limits.store';
import { NotificationsStore } from './notifications.store';
import { WalletStore } from './wallet.store';

const note = (id: number, type: NotificationType = 'DailyBonus', isRead = false): NotificationDto => ({
  id,
  type,
  title: `Title ${id}`,
  message: `Message ${id}`,
  isRead,
  createdAt: '2026-10-25T12:00:00Z'
});

describe('NotificationsStore (the bell)', () => {
  let store: InstanceType<typeof NotificationsStore>;
  let api: jasmine.SpyObj<NotificationsApi>;
  let auth: { refresh: jasmine.Spy; logout: jasmine.Spy; role: ReturnType<typeof signal<UserRole | null>> };
  let router: { url: string; navigateByUrl: jasmine.Spy };
  let toasts: { success: jasmine.Spy; info: jasmine.Spy; error: jasmine.Spy };

  beforeEach(() => {
    api = jasmine.createSpyObj<NotificationsApi>('NotificationsApi', ['list', 'markRead', 'markAllRead']);
    api.list.and.returnValue(of({ items: [note(2), note(1, 'DailyBonus', true)], page: 1, pageSize: 20, totalCount: 2, totalPages: 1, unreadCount: 1 }));
    api.markRead.and.returnValue(of(undefined));
    api.markAllRead.and.returnValue(of({ updated: 1 }));

    auth = { refresh: jasmine.createSpy('refresh').and.resolveTo(true), logout: jasmine.createSpy('logout').and.resolveTo(), role: signal<UserRole | null>('Player') };
    router = { url: '/crash', navigateByUrl: jasmine.createSpy('navigateByUrl').and.resolveTo(true) };
    toasts = jasmine.createSpyObj('ToastStore', ['success', 'info', 'error']);

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: NotificationsApi, useValue: api },
        { provide: AuthStore, useValue: auth },
        { provide: Router, useValue: router },
        { provide: ToastStore, useValue: toasts },
        { provide: WalletStore, useValue: { load: jasmine.createSpy('load').and.resolveTo() } },
        { provide: LimitsStore, useValue: { load: jasmine.createSpy('load').and.resolveTo() } }
      ]
    });
    store = TestBed.inject(NotificationsStore);
  });

  it('loads the latest notifications and the unread count', async () => {
    await store.load();
    expect(store.items().map((n) => n.id)).toEqual([2, 1]);
    expect(store.unreadCount()).toBe(1);
    expect(store.loaded()).toBeTrue();
  });

  it('a live notification goes on top, raises the badge and shows a toast', async () => {
    await store.load();
    await store.receive(note(3));
    expect(store.items()[0].id).toBe(3);
    expect(store.unreadCount()).toBe(2);
    expect(toasts.info).toHaveBeenCalledWith('Title 3');
  });

  it('ignores a notification it already has (pushed while the list was loading)', async () => {
    await store.load();
    await store.receive(note(2));
    expect(store.items().length).toBe(2);
    expect(store.unreadCount()).toBe(1);
  });

  it('marking one as read lowers the badge at once (optimistic) and tells the server', async () => {
    await store.load();
    await store.markRead(2);
    expect(store.unreadCount()).toBe(0);
    expect(store.items()[0].isRead).toBeTrue();
    expect(api.markRead).toHaveBeenCalledOnceWith(2);

    // Already read: no second request
    await store.markRead(2);
    expect(api.markRead).toHaveBeenCalledTimes(1);
  });

  it('mark all read clears the badge', async () => {
    await store.load();
    await store.receive(note(3));
    await store.markAllRead();
    expect(store.unreadCount()).toBe(0);
    expect(store.items().every((n) => n.isRead)).toBeTrue();
    expect(api.markAllRead).toHaveBeenCalled();
  });

  it('a role change refreshes the token, and leaves a page the new role may not see', async () => {
    router.url = '/admin/users';
    auth.refresh.and.callFake(async () => {
      auth.role.set('Player'); // the new token says Player
      return true;
    });

    await store.receive(note(5, 'RoleChanged'));

    expect(auth.refresh).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/crash');
  });

  it('a block signs the user out immediately', async () => {
    await store.receive({ ...note(6, 'AccountBlocked'), message: 'An administrator blocked your account.' });
    expect(toasts.error).toHaveBeenCalledWith('An administrator blocked your account.');
    expect(auth.logout).toHaveBeenCalled();
  });
});
