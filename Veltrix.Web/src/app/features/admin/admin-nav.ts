import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { Icons } from '../../shared/ui';

interface AdminLink {
  label: string;
  path: string;
  icon: LucideIconData;
  exact: boolean;
}

const LINKS: AdminLink[] = [
  { label: 'Dashboard', path: '/admin', icon: Icons.dashboard, exact: true },
  { label: 'Users', path: '/admin/users', icon: Icons.users, exact: false },
  { label: 'Audit log', path: '/admin/audit', icon: Icons.audit, exact: false },
  { label: 'Settings', path: '/admin/settings', icon: Icons.settings, exact: false }
];

/** The admin sections as segmented links (same look as vx-tabs, but real links: each section has a URL). */
@Component({
  selector: 'vx-admin-nav',
  imports: [RouterLink, RouterLinkActive, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'mb-6 flex max-w-full' },
  template: `
    <nav class="flex max-w-full gap-1 overflow-x-auto rounded-control border border-border bg-surface p-1" aria-label="Admin sections">
      @for (link of links; track link.path) {
        <a
          [routerLink]="link.path"
          routerLinkActive
          #rla="routerLinkActive"
          [routerLinkActiveOptions]="{ exact: link.exact }"
          [attr.aria-current]="rla.isActive ? 'page' : null"
          [class]="rla.isActive ? active : inactive"
        >
          <lucide-icon [img]="link.icon" [size]="15" aria-hidden="true" />
          {{ link.label }}
        </a>
      }
    </nav>
  `
})
export class AdminNav {
  protected readonly links = LINKS;
  private readonly base =
    'inline-flex h-8 shrink-0 items-center gap-2 rounded-lg px-3.5 text-[13px] font-medium whitespace-nowrap transition-colors duration-200 ';
  protected readonly active = this.base + 'bg-surface-2 text-text shadow-soft';
  protected readonly inactive = this.base + 'text-muted hover:text-text';
}
