import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Top of every page: the page's <h1>, a one-line description, and actions ([page-actions]).
 * On desktop the top bar already shows the title, so the h1 is visually hidden there
 * (still read by screen readers); on phones it is visible.
 */
@Component({
  selector: 'vx-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'mb-6 flex flex-wrap items-end justify-between gap-4' },
  template: `
    <div class="min-w-0">
      <h1 class="text-2xl font-semibold tracking-tight md:sr-only">{{ title() }}</h1>
      @if (description()) {
        <p class="mt-1 text-sm text-muted md:mt-0">{{ description() }}</p>
      }
    </div>
    <div class="flex flex-wrap items-center gap-2 empty:hidden"><ng-content select="[page-actions]" /></div>
  `
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly description = input<string>();
}
