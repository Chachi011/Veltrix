import { ChangeDetectionStrategy, Component, ElementRef, inject, input, model } from '@angular/core';

export interface TabItem {
  value: string;
  label: string;
}

/**
 * Segmented tabs: <vx-tabs [items]="tabs" [(value)]="tab" />
 * Keyboard: ← → move between tabs (WAI-ARIA tabs pattern).
 */
@Component({
  selector: 'vx-tabs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex max-w-full' },
  template: `
    <div class="flex max-w-full gap-1 overflow-x-auto rounded-control border border-border bg-surface p-1" role="tablist">
      @for (item of items(); track item.value) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="item.value === value()"
          [attr.tabindex]="item.value === value() ? 0 : -1"
          class="h-8 shrink-0 rounded-lg px-3.5 text-[13px] font-medium whitespace-nowrap transition-colors duration-200"
          [class]="item.value === value() ? 'bg-surface-2 text-text shadow-soft' : 'text-muted hover:text-text'"
          (click)="value.set(item.value)"
          (keydown)="onKey($event)"
        >
          {{ item.label }}
        </button>
      }
    </div>
  `
})
export class Tabs {
  readonly items = input.required<TabItem[]>();
  readonly value = model.required<string>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected onKey(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
      return;
    }
    event.preventDefault();

    const items = this.items();
    const index = items.findIndex((i) => i.value === this.value());
    const next = (index + (event.key === 'ArrowRight' ? 1 : -1) + items.length) % items.length;
    this.value.set(items[next].value);
    this.host.nativeElement.querySelectorAll<HTMLButtonElement>('[role="tab"]')[next]?.focus();
  }
}
