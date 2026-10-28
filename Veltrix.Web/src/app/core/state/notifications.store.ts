import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { NotificationsApi } from '../api/notifications.api';
import { AuthStore } from '../auth/auth.store';
import { NotificationDto, UserRole } from '../models/api.models';
import { I18n } from '../i18n/i18n';
import { ToastStore } from '../ui/toast.store';
import { LimitsStore } from './limits.store';
import { WalletStore } from './wallet.store';

/** The bell shows the latest notifications; older ones are not needed in a dropdown. */
export const NOTIFICATIONS_PAGE_SIZE = 20;

type NotificationsState = {
  items: NotificationDto[];
  unreadCount: number;
  loaded: boolean;
};

const initialState: NotificationsState = { items: [], unreadCount: 0, loaded: false };

/** Which sections a role may see; used after a role change to leave a page that is no longer allowed. */
const SECTION_ROLES: { prefix: string; roles: UserRole[] }[] = [
  { prefix: '/admin', roles: ['Admin'] },
  { prefix: '/operator', roles: ['Operator', 'Admin'] },
  { prefix: '/limits', roles: ['Player'] }
];

/**
 * The bell: the latest notifications and the unread count.
 * New notifications arrive live (SignalR "Notification" event) and are prepended here; some types also
 * change the session right away (a new role refreshes the token, a block signs the user out).
 */
export const NotificationsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ unreadCount }) => ({
    /** "9+" so the badge stays small. */
    badge: computed(() => (unreadCount() > 9 ? '9+' : String(unreadCount())))
  })),
  withMethods(
    (
      store,
      api = inject(NotificationsApi),
      auth = inject(AuthStore),
      wallet = inject(WalletStore),
      limits = inject(LimitsStore),
      toasts = inject(ToastStore),
      i18n = inject(I18n),
      router = inject(Router)
    ) => {
      /** What a notification changes in the running app, besides the bell. */
      const react = async (notification: NotificationDto): Promise<void> => {
        switch (notification.type) {
          case 'AccountBlocked':
            // The server already revoked every session; leave cleanly instead of failing on the next request
            toasts.error(i18n.server(notification.message));
            await auth.logout();
            return;

          case 'RoleChanged': {
            // A fresh token carries the new role, so menus and route guards match it at once
            await auth.refresh();
            const role = auth.role();
            const section = SECTION_ROLES.find((s) => router.url.startsWith(s.prefix));
            if (section && (role === null || !section.roles.includes(role))) {
              await router.navigateByUrl('/crash');
            }
            break;
          }

          case 'SelfExcluded':
            await limits.load().catch(() => undefined);
            break;

          case 'DailyBonus':
          case 'BalanceAdjusted':
            await wallet.load().catch(() => undefined);
            break;
        }
        toasts.info(i18n.server(notification.title));
      };

      return {
        async load(): Promise<void> {
          const page = await firstValueFrom(api.list(1, NOTIFICATIONS_PAGE_SIZE));
          patchState(store, { items: page.items, unreadCount: page.unreadCount, loaded: true });
        },

        /** A live notification from SignalR. */
        async receive(notification: NotificationDto): Promise<void> {
          if (store.items().some((n) => n.id === notification.id)) {
            return; // already loaded (e.g. arrived while the list was being fetched)
          }
          patchState(store, (s) => ({
            items: [notification, ...s.items].slice(0, NOTIFICATIONS_PAGE_SIZE),
            unreadCount: s.unreadCount + (notification.isRead ? 0 : 1)
          }));
          await react(notification);
        },

        /** Optimistic: the badge updates immediately; a failed request is simply corrected by the next load. */
        async markRead(id: number): Promise<void> {
          const target = store.items().find((n) => n.id === id);
          if (!target || target.isRead) {
            return;
          }
          patchState(store, (s) => ({
            items: s.items.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
            unreadCount: Math.max(0, s.unreadCount - 1)
          }));
          await firstValueFrom(api.markRead(id)).catch(() => undefined);
        },

        async markAllRead(): Promise<void> {
          if (store.unreadCount() === 0) {
            return;
          }
          patchState(store, (s) => ({ items: s.items.map((n) => ({ ...n, isRead: true })), unreadCount: 0 }));
          await firstValueFrom(api.markAllRead()).catch(() => undefined);
        },

        reset(): void {
          patchState(store, initialState);
        }
      };
    }
  )
);
