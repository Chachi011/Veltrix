import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { Router } from '@angular/router';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { NotificationDto, NotificationType } from '../core/models/api.models';
import { NotificationsStore } from '../core/state/notifications.store';
import { TimeAgoPipe } from '../shared/pipes';
// Single files, not the shared/ui barrel: the layout is in the initial bundle, the barrel would pull in every component
import { Button } from '../shared/ui/button';
import { EmptyState } from '../shared/ui/empty-state';
import { Icons } from '../shared/ui/icons';
import { Skeleton } from '../shared/ui/skeleton';

interface TypeStyle {
  icon: LucideIconData;
  /** Complete class strings (Tailwind only generates classes it finds literally). */
  tone: string;
  /** Where a click on the notification leads. */
  link: string | null;
}

const STYLES: Record<NotificationType, TypeStyle> = {
  DailyBonus: { icon: Icons.gift, tone: 'bg-primary/12 text-primary-text', link: '/wallet' },
  BalanceAdjusted: { icon: Icons.vlx, tone: 'bg-accent/12 text-accent', link: '/wallet' },
  RoleChanged: { icon: Icons.userCog, tone: 'bg-primary/12 text-primary-text', link: null },
  AccountBlocked: { icon: Icons.block, tone: 'bg-danger/12 text-danger', link: null },
  AccountUnblocked: { icon: Icons.unlock, tone: 'bg-success/12 text-success', link: null },
  PasswordChanged: { icon: Icons.key, tone: 'bg-warning/12 text-warning', link: '/profile' },
  SelfExcluded: { icon: Icons.pause, tone: 'bg-warning/12 text-warning', link: '/limits' }
};

const FALLBACK: TypeStyle = { icon: Icons.bell, tone: 'bg-surface-2 text-muted', link: null };

/** Content of the bell dropdown: newest first, unread ones marked, "Mark all read". */
@Component({
  selector: 'vx-notifications-panel',
  imports: [LucideAngularModule, Button, EmptyState, Skeleton, TimeAgoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between gap-3 border-b border-border py-2.5 pr-2 pl-4">
      <h2 class="text-sm font-semibold">
        Notifications
        @if (store.unreadCount() > 0) {
          <span class="num ml-1 text-xs font-medium text-muted">{{ store.unreadCount() }} new</span>
        }
      </h2>
      <button vxButton type="button" variant="ghost" size="sm" [disabled]="store.unreadCount() === 0" (click)="store.markAllRead()">
        <lucide-icon [img]="icons.markAllRead" [size]="15" aria-hidden="true" />
        Mark all read
      </button>
    </div>

    @if (!store.loaded()) {
      <div class="flex flex-col gap-4 p-4">
        @for (row of [1, 2, 3]; track row) {
          <div class="flex gap-3">
            <vx-skeleton width="32px" height="32px" shape="circle" />
            <div class="flex flex-1 flex-col gap-2">
              <vx-skeleton width="60%" height="0.75rem" />
              <vx-skeleton width="90%" height="0.75rem" />
            </div>
          </div>
        }
      </div>
    } @else if (store.items().length === 0) {
      <vx-empty-state [icon]="icons.bell" title="You're all caught up" message="Bonuses and account changes will appear here." />
    } @else {
      <ul class="max-h-[min(420px,calc(100vh-160px))] overflow-y-auto py-1">
        @for (n of store.items(); track n.id) {
          <li>
            <button
              type="button"
              [class]="n.isRead ? itemClass : itemClass + ' bg-primary/5'"
              (click)="open(n)"
            >
              <span [class]="'mt-0.5 grid size-8 shrink-0 place-items-center rounded-full ' + style(n).tone">
                <lucide-icon [img]="style(n).icon" [size]="15" aria-hidden="true" />
              </span>
              <span class="min-w-0 flex-1">
                <span class="flex items-start justify-between gap-2">
                  <span class="text-[13px] font-semibold" [class.text-muted]="n.isRead">{{ n.title }}</span>
                  @if (!n.isRead) {
                    <span class="mt-1.5 size-2 shrink-0 rounded-full bg-primary" aria-hidden="true"></span>
                    <span class="sr-only">(unread)</span>
                  }
                </span>
                <span class="mt-0.5 line-clamp-2 block text-xs text-muted">{{ n.message }}</span>
                <span class="mt-1 block text-[11px] text-muted">{{ n.createdAt | timeAgo }}</span>
              </span>
            </button>
          </li>
        }
      </ul>
    }
  `
})
export class NotificationsPanel {
  protected readonly store = inject(NotificationsStore);
  private readonly router = inject(Router);
  protected readonly icons = Icons;
  protected readonly itemClass =
    'flex w-full gap-3 px-4 py-3 text-left transition-colors duration-150 hover:bg-surface-2 focus-visible:bg-surface-2';

  /** Asks the top bar to close the dropdown (after following a link). */
  readonly closed = output<void>();

  protected style(n: NotificationDto): TypeStyle {
    return STYLES[n.type] ?? FALLBACK;
  }

  protected open(n: NotificationDto): void {
    void this.store.markRead(n.id);
    const link = this.style(n).link;
    if (link) {
      this.closed.emit();
      void this.router.navigateByUrl(link);
    }
  }
}
