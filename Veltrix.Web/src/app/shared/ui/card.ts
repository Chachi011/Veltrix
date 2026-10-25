import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Surface for grouping content. Optional title / subtitle header; actions go in an element with [card-actions].
 * <vx-card title="Transactions"><button card-actions ...>Export</button> ...content... </vx-card>
 */
@Component({
  selector: 'vx-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // min-w-0: as a grid/flex child, a card may shrink below its content (wide tables scroll inside instead)
  host: { class: 'block min-w-0 rounded-card border border-border bg-surface shadow-card' },
  template: `
    @if (title() || subtitle()) {
      <header class="flex items-start justify-between gap-4 px-5 pt-5 md:px-6">
        <div class="min-w-0">
          @if (title()) {
            <h3 class="text-[15px] leading-6 font-semibold">{{ title() }}</h3>
          }
          @if (subtitle()) {
            <p class="mt-0.5 text-[13px] text-muted">{{ subtitle() }}</p>
          }
        </div>
        <ng-content select="[card-actions]" />
      </header>
    }
    <div [class]="bodyClass()">
      <ng-content />
    </div>
  `
})
export class Card {
  readonly title = input<string>();
  readonly subtitle = input<string>();
  /** 'none' for edge-to-edge content such as tables. */
  readonly padding = input<'default' | 'none'>('default');

  protected readonly bodyClass = computed(() => {
    if (this.padding() === 'none') {
      return this.title() ? 'pt-4' : '';
    }
    return this.title() || this.subtitle() ? 'px-5 pt-4 pb-5 md:px-6 md:pb-6' : 'p-5 md:p-6';
  });
}
