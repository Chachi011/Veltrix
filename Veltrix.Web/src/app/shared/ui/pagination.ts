import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { Button } from './button';
import { Icons } from './icons';

/** Card footer with "Page 2 of 7 · 98 results" and previous/next buttons. Hidden when there is only one page. */
@Component({
  selector: 'vx-pagination',
  imports: [LucideAngularModule, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="flex items-center justify-between gap-3 border-t border-border px-5 py-3 md:px-6" [attr.aria-label]="'Pagination' | t">
        <p class="text-xs text-muted">
          {{ 'Page {page} of {total}' | t: { page: page(), total: totalPages() } }}
          @if (totalCount() !== null) {
            <span class="hidden sm:inline"> · {{ '{count} results' | t: { count: totalCount()! } }}</span>
          }
        </p>
        <div class="flex gap-2">
          <button vxButton type="button" size="sm" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
            <lucide-icon [img]="icons.chevronLeft" [size]="14" aria-hidden="true" />{{ 'Previous' | t }}
          </button>
          <button vxButton type="button" size="sm" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">
            {{ 'Next' | t }}<lucide-icon [img]="icons.chevronRight" [size]="14" aria-hidden="true" />
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
