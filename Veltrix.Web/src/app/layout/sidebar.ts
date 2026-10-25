import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { AuthStore } from '../core/auth/auth.store';
import { LayoutService } from '../core/ui/layout.service';
import { Icons } from '../shared/ui/icons';
import { NAV_ITEMS } from './nav-items';

/** Desktop navigation (hidden below 768 px, where the bottom nav takes over). */
@Component({
  selector: 'vx-sidebar',
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside
      class="fixed inset-y-0 left-0 z-40 hidden flex-col border-r border-border bg-surface transition-[width] duration-200 ease-out-soft md:flex"
      [class]="layout.sidebarCollapsed() ? 'w-[72px]' : 'w-60'"
      aria-label="Main navigation"
    >
      <a routerLink="/crash" class="flex h-16 shrink-0 items-center gap-3 px-5" aria-label="VELTRIX home">
        <span class="grid size-8 shrink-0 place-items-center rounded-control bg-primary text-sm font-bold text-on-primary">V</span>
        @if (!layout.sidebarCollapsed()) {
          <span class="text-[15px] font-semibold tracking-[0.18em] text-text">VELTRIX</span>
        }
      </a>

      <nav class="flex flex-1 flex-col gap-1 px-3 py-4">
        @for (item of items(); track item.path) {
          <a
            [routerLink]="item.path"
            routerLinkActive
            #rla="routerLinkActive"
            [attr.aria-current]="rla.isActive ? 'page' : null"
            [title]="layout.sidebarCollapsed() ? item.label : ''"
            class="group relative flex h-10 items-center gap-3 rounded-control px-3 text-sm font-medium transition-colors duration-200"
            [class]="rla.isActive ? 'bg-primary/12 text-text' : 'text-muted hover:bg-surface-2 hover:text-text'"
          >
            @if (rla.isActive) {
              <span class="absolute top-2 bottom-2 -left-3 w-0.5 rounded-full bg-primary" aria-hidden="true"></span>
            }
            <lucide-icon
              [img]="item.icon"
              [size]="18"
              [class]="rla.isActive ? 'text-primary-text' : ''"
              aria-hidden="true"
            />
            @if (!layout.sidebarCollapsed()) {
              <span class="truncate">{{ item.label }}</span>
            }
          </a>
        }
      </nav>

      <div class="border-t border-border p-3">
        @if (!layout.sidebarCollapsed()) {
          <p class="mb-3 px-2 text-[11px] leading-4 text-muted">
            Educational project. VLX points are virtual and have no monetary value.
          </p>
        }
        <button
          type="button"
          class="flex h-9 w-full items-center gap-3 rounded-control px-3 text-[13px] text-muted transition-colors duration-200 hover:bg-surface-2 hover:text-text"
          (click)="layout.toggleSidebar()"
          [attr.aria-label]="layout.sidebarCollapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
          [attr.aria-expanded]="!layout.sidebarCollapsed()"
        >
          <lucide-icon [img]="layout.sidebarCollapsed() ? icons.sidebarOpen : icons.sidebarClose" [size]="18" aria-hidden="true" />
          @if (!layout.sidebarCollapsed()) {
            <span>Collapse</span>
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

  protected readonly items = computed(() => NAV_ITEMS.filter((item) => !item.auth || this.auth.isLoggedIn()));
}
