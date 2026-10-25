import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideAngularModule, LucideIconData } from 'lucide-angular';
import { Icons } from './icons';

/** "Nothing here yet": an icon, a title and ONE helpful line, plus an optional action (projected). */
@Component({
  selector: 'vx-empty-state',
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col items-center px-6 py-10 text-center' },
  template: `
    <span class="grid size-12 place-items-center rounded-full border border-border bg-surface-2 text-muted">
      <lucide-icon [img]="icon()" [size]="22" aria-hidden="true" />
    </span>
    <h4 class="mt-4 text-sm font-semibold">{{ title() }}</h4>
    @if (message()) {
      <p class="mt-1 max-w-xs text-[13px] text-muted">{{ message() }}</p>
    }
    <div class="mt-4 empty:hidden"><ng-content /></div>
  `
})
export class EmptyState {
  readonly icon = input<LucideIconData>(Icons.inbox);
  readonly title = input.required<string>();
  readonly message = input<string>();
}
