import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { AuthStore } from '../../core/auth/auth.store';
import { AdminUserListItemDto, PagedResult, UserRole } from '../../core/models/api.models';
import { VlxPipe } from '../../shared/pipes';
import { Avatar, Badge, Card, EmptyState, Icons, ModalService, PageHeader, Pagination, Skeleton } from '../../shared/ui';
import { AdminNav } from './admin-nav';
import { ROLE_TONES, UserDrawer } from './user-drawer';

const PAGE_SIZE = 15;
const SEARCH_DELAY_MS = 300;

/** Admin users table: search, role and status filters, paging; a row opens the user drawer. */
@Component({
  selector: 'vx-admin-users-page',
  imports: [DatePipe, FormsModule, LucideAngularModule, VlxPipe, AdminNav, Avatar, Badge, Card, EmptyState, PageHeader, Pagination, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-users-page.html'
})
export class AdminUsersPage {
  private readonly api = inject(AdminApi);
  private readonly modal = inject(ModalService);
  protected readonly auth = inject(AuthStore);

  protected readonly icons = Icons;
  protected readonly roleTones = ROLE_TONES;
  protected readonly roles: UserRole[] = ['Player', 'Operator', 'Admin'];
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];

  protected readonly searchText = signal('');
  /** The search actually sent, 300 ms after the last keystroke (not one request per letter). */
  private readonly search = signal('');
  protected readonly role = signal<UserRole | null>(null);
  protected readonly blocked = signal<boolean | null>(null);
  protected readonly page = signal(1);

  protected readonly users = signal<PagedResult<AdminUserListItemDto> | null>(null);
  protected readonly loading = signal(false);

  constructor() {
    let timer: ReturnType<typeof setTimeout> | undefined;
    effect(() => {
      const text = this.searchText().trim();
      clearTimeout(timer);
      timer = setTimeout(() => {
        this.search.set(text);
        this.page.set(1);
      }, SEARCH_DELAY_MS);
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(timer));

    effect(() => {
      const query = { page: this.page(), pageSize: PAGE_SIZE, search: this.search(), role: this.role(), isBlocked: this.blocked() };
      untracked(() => void this.load(query));
    });
  }

  protected setRole(role: UserRole | null): void {
    this.role.set(role);
    this.page.set(1);
  }

  protected setBlocked(value: boolean | null): void {
    this.blocked.set(value);
    this.page.set(1);
  }

  protected async open(user: AdminUserListItemDto): Promise<void> {
    const ref = this.modal.openDrawer<void, number, UserDrawer>(UserDrawer, {
      data: user.id,
      ariaLabel: `User ${user.nickname}`,
      width: '520px'
    });
    await firstValueFrom(ref.closed);
    // Role, status or balance may have changed in the drawer
    await this.load({ page: this.page(), pageSize: PAGE_SIZE, search: this.search(), role: this.role(), isBlocked: this.blocked() });
  }

  private async load(query: Parameters<AdminApi['users']>[0]): Promise<void> {
    this.loading.set(true);
    try {
      this.users.set(await firstValueFrom(this.api.users(query)));
    } catch {
      // Shown by the error interceptor
    } finally {
      this.loading.set(false);
    }
  }
}
