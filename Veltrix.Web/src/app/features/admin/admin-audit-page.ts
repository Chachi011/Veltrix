import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { AuditLogDto, AuditLogQuery, PagedResult } from '../../core/models/api.models';
import { Badge, BadgeTone, Button, Card, EmptyState, Icons, PageHeader, Pagination, Skeleton } from '../../shared/ui';
import { AdminNav } from './admin-nav';
import { describeChange, prettyJson } from './audit-format';

const PAGE_SIZE = 20;
const ENTITY_TYPES = ['User', 'Game', 'GameRound', 'SystemSetting'];

/** Colour by what kind of action it was (complete class strings via the Badge tones). */
function actionTone(action: string): BadgeTone {
  if (/Unblocked|Created/.test(action)) return 'success';
  if (/Blocked|Cancelled/.test(action)) return 'danger';
  if (/Balance|Setting|SelfExclusion/.test(action)) return 'warning';
  if (/Role/.test(action)) return 'primary';
  return 'neutral';
}

/** "UserRoleChanged" → "User role changed". */
function actionLabel(action: string): string {
  const words = action.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

/**
 * The audit log: who changed what and when, newest first. Read-only, the server never edits or deletes rows.
 * Filters live in the URL, so the user drawer can link to "everything done to user 5".
 */
@Component({
  selector: 'vx-admin-audit-page',
  imports: [DatePipe, FormsModule, LucideAngularModule, AdminNav, Badge, Button, Card, EmptyState, PageHeader, Pagination, Skeleton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-audit-page.html'
})
export class AdminAuditPage {
  private readonly api = inject(AdminApi);
  private readonly router = inject(Router);

  protected readonly icons = Icons;
  protected readonly entityTypes = ENTITY_TYPES;
  protected readonly actionTone = actionTone;
  protected readonly actionLabel = actionLabel;
  protected readonly describe = describeChange;
  protected readonly pretty = prettyJson;
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6];

  private readonly params = inject(ActivatedRoute).snapshot.queryParamMap;
  protected readonly action = signal<string | null>(this.params.get('action'));
  protected readonly entityType = signal<string | null>(this.params.get('entityType'));
  protected readonly entityId = signal<string | null>(this.params.get('entityId'));
  protected readonly page = signal(1);

  protected readonly actions = signal<string[]>([]);
  protected readonly logs = signal<PagedResult<AuditLogDto> | null>(null);
  protected readonly loading = signal(false);
  protected readonly expanded = signal<number | null>(null);

  constructor() {
    void firstValueFrom(this.api.auditActions())
      .then((actions) => this.actions.set(actions))
      .catch(() => undefined);

    effect(() => {
      const query: AuditLogQuery = {
        page: this.page(),
        pageSize: PAGE_SIZE,
        action: this.action(),
        entityType: this.entityType(),
        entityId: this.entityId()
      };
      untracked(() => {
        void this.router.navigate([], {
          queryParams: { action: query.action, entityType: query.entityType, entityId: query.entityId },
          replaceUrl: true
        });
        void this.load(query);
      });
    });
  }

  protected setFilter(kind: 'action' | 'entityType', value: string | null): void {
    (kind === 'action' ? this.action : this.entityType).set(value);
    if (kind === 'entityType') {
      this.entityId.set(null); // an id only makes sense together with its entity type
    }
    this.page.set(1);
  }

  protected clear(): void {
    this.action.set(null);
    this.entityType.set(null);
    this.entityId.set(null);
    this.page.set(1);
  }

  protected toggle(id: number): void {
    this.expanded.update((current) => (current === id ? null : id));
  }

  private async load(query: AuditLogQuery): Promise<void> {
    this.loading.set(true);
    try {
      this.logs.set(await firstValueFrom(this.api.auditLogs(query)));
      this.expanded.set(null);
    } catch {
      // Shown by the error interceptor
    } finally {
      this.loading.set(false);
    }
  }
}
