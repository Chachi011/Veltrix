import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { CopyButton } from './copy-button';
import { Icons } from './icons';

/**
 * "Provably fair" chip with the round's published hash (truncated) and a copy button.
 * With a roundId, the label links to that round's verification page.
 */
@Component({
  selector: 'vx-hash-badge',
  imports: [LucideAngularModule, RouterLink, CopyButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'inline-flex h-8 max-w-full items-center gap-2 rounded-full border border-border bg-surface-2 pl-2.5 pr-0.5 text-xs'
  },
  template: `
    <lucide-icon [img]="icons.shield" [size]="14" class="shrink-0 text-success" aria-hidden="true" />
    @if (roundId(); as id) {
      <a
        routerLink="/fairness"
        [queryParams]="{ round: id }"
        class="font-medium whitespace-nowrap text-muted transition-colors duration-200 hover:text-text"
        >{{ label() }}</a
      >
    } @else {
      <span class="font-medium whitespace-nowrap text-muted">{{ label() }}</span>
    }
    <span class="num truncate text-text" [title]="hash()">{{ short() }}</span>
    <vx-copy-button [text]="hash()" label="Copy hash" />
  `
})
export class HashBadge {
  readonly hash = input.required<string>();
  readonly roundId = input<number | null>(null);
  readonly label = input('Provably fair');

  protected readonly icons = Icons;
  protected readonly short = computed(() => {
    const h = this.hash();
    return h.length > 14 ? `${h.slice(0, 8)}…${h.slice(-4)}` : h;
  });
}
