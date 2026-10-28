import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../core/auth/auth.store';
import { TranslatePipe } from '../core/i18n/i18n-pipes';
import { LayoutService } from '../core/ui/layout.service';
import { Icons } from '../shared/ui/icons';
import { visibleNavItems } from './nav-items';

/** Desktop navigation (hidden below 768 px, where the bottom nav takes over). */
@Component({
  selector: 'vx-sidebar',
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, LucideAngularModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside
      class="fixed inset-y-0 left-0 z-40 hidden flex-col border-r border-border bg-surface transition-[width] duration-200 ease-out-soft md:flex"
      [class]="layout.sidebarCollapsed() ? 'w-[72px]' : 'w-60'"
      [attr.aria-label]="'Main navigation' | t"
    >
      <a routerLink="/" class="flex h-16 shrink-0 items-center gap-3 px-5" [attr.aria-label]="'VELTRIX home' | t">
        <span class="grid size-8 shrink-0 place-items-center rounded-control bg-primary text-sm font-bold text-on-primary">V</span>
        @if (!layout.sidebarCollapsed()) {
          <span class="text-[15px] font-semibold tracking-[0.18em] text-text">VELTRIX</span>
        }
      </a>

      <nav class="flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto px-3 py-4">
        @for (item of items(); track item.path) {
          <ng-container *ngTemplateOutlet="link; context: { $implicit: item }" />
        }

        <!-- Staff tools: only visible to Operator / Admin (the server checks the role again) -->
        @if (staffItems().length > 0) {
          @if (layout.sidebarCollapsed()) {
            <div class="mx-2 my-2 h-px bg-border" role="separator"></div>
          } @else {
            <p class="mt-4 mb-1 px-3 text-[11px] font-semibold tracking-wide text-muted uppercase">{{ 'Staff' | t }}</p>
          }
          @for (item of staffItems(); track item.path) {
            <ng-container *ngTemplateOutlet="link; context: { $implicit: item }" />
          }
        }
      </nav>

      <ng-template #link let-item>
        <a
          [routerLink]="item.path"
          routerLinkActive
          #rla="routerLinkActive"
          [routerLinkActiveOptions]="{ exact: item.path === '/' }"
          [attr.aria-current]="rla.isActive ? 'page' : null"
          [title]="layout.sidebarCollapsed() ? (item.label | t) : ''"
          class="group relative flex h-10 shrink-0 items-center gap-3 rounded-control px-3 text-sm font-medium transition-colors duration-200"
          [class]="rla.isActive ? 'bg-primary/12 text-text' : 'text-muted hover:bg-surface-2 hover:text-text'"
        >
          @if (rla.isActive) {
            <span class="absolute top-2 bottom-2 -left-3 w-0.5 rounded-full bg-primary" aria-hidden="true"></span>
          }
          <lucide-icon [img]="item.icon" [size]="18" [class]="rla.isActive ? 'text-primary-text' : ''" aria-hidden="true" />
          @if (!layout.sidebarCollapsed()) {
            <span class="truncate">{{ item.label | t }}</span>
          }
        </a>
      </ng-template>

      <div class="border-t border-border p-3">
        @if (!layout.sidebarCollapsed()) {
          <p class="mb-3 px-2 text-[11px] leading-4 text-muted">
            {{ 'Educational project. VLX points are virtual and have no monetary value.' | t }}
          </p>
        }
        <button
          type="button"
          class="flex h-9 w-full items-center gap-3 rounded-control px-3 text-[13px] text-muted transition-colors duration-200 hover:bg-surface-2 hover:text-text"
          (click)="layout.toggleSidebar()"
          [attr.aria-label]="layout.sidebarCollapsed() ? ('Expand sidebar' | t) : ('Collapse sidebar' | t)"
          [attr.aria-expanded]="!layout.sidebarCollapsed()"
        >
          <lucide-icon [img]="layout.sidebarCollapsed() ? icons.sidebarOpen : icons.sidebarClose" [size]="18" aria-hidden="true" />
          @if (!layout.sidebarCollapsed()) {
            <span>{{ 'Collapse' | t }}</span>
          }
        </button>
      </div>
    </aside>
  `
})
export class Sidebar {
  protected readonly layout = inject(LayoutService);
  private readonly auth = inject(AuthStore);
  protected readonly icons = Icons;

  private readonly visible = computed(() => visibleNavItems(this.auth.role()));
  protected readonly items = computed(() => this.visible().filter((item) => item.section !== 'staff' && !item.menuOnly));
  protected readonly staffItems = computed(() => this.visible().filter((item) => item.section === 'staff'));
}
