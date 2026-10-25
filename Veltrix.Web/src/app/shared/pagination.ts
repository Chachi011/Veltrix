import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'vx-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="pager" aria-label="Pagination">
        <button class="btn btn-sm" type="button" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
          ‹ Prev
        </button>
        <span class="muted num">Page {{ page() }} of {{ totalPages() }}</span>
        <button class="btn btn-sm" type="button" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">
          Next ›
        </button>
      </nav>
    }
  `,
  styles: `
    .pager {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      margin-top: 16px;
    }
  `
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();
}
