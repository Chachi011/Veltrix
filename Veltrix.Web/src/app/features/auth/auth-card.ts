import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { Card } from '../../shared/ui/card';
import { Icons } from '../../shared/ui/icons';

/** Shared frame of the login / register / reset pages: icon, title, subtitle, card, footer ([auth-footer]). */
@Component({
  selector: 'vx-auth-card',
  imports: [LucideAngularModule, Card],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'mx-auto block w-full max-w-[420px] pt-2 md:pt-8' },
  template: `
    <div class="mb-6 flex flex-col items-center text-center">
      <span class="grid size-12 place-items-center rounded-card border border-primary/20 bg-primary/12 text-primary-text">
        <lucide-icon [img]="icon()" [size]="22" aria-hidden="true" />
      </span>
      <h1 class="mt-4 text-2xl font-semibold tracking-tight">{{ title() }}</h1>
      @if (subtitle()) {
        <p class="mt-1 text-sm text-muted">{{ subtitle() }}</p>
      }
    </div>
    <vx-card>
      <ng-content />
    </vx-card>
    <div class="mt-6 text-center text-sm text-muted empty:hidden"><ng-content select="[auth-footer]" /></div>
  `
})
export class AuthCard {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly icon = input<LucideIconData>(Icons.user);
}
