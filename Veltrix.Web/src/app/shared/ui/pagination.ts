import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Button } from './button';
import { Icons } from './icons';

/** Card footer with "Page 2 of 7 · 98 results" and previous/next buttons. Hidden when there is only one page. */
@Component({
  selector: 'vx-pagination',
  imports: [LucideAngularModule, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="flex items-center justify-between gap-3 border-t border-border px-5 py-3 md:px-6" aria-label="Pagination">
        <p class="text-xs text-muted">
          Page <span class="num text-text">{{ page() }}</span> of <span class="num">{{ totalPages() }}</span>
          @if (totalCount() !== null) {
            <span class="hidden sm:inline"> · <span class="num">{{ totalCount() }}</span> results</span>
          }
        </p>
        <div class="flex gap-2">
          <button vxButton type="button" size="sm" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
            <lucide-icon [img]="icons.chevronLeft" [size]="14" aria-hidden="true" />Previous
          </button>
          <button vxButton type="button" size="sm" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">
            Next<lucide-icon [img]="icons.chevronRight" [size]="14" aria-hidden="true" />
          </button>
        </div>
      </nav>
    }
  `
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input<number | null>(null);
  readonly pageChange = output<number>();

  protected readonly icons = Icons;
}
