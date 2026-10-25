import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { Button, Card, EmptyState, Icons } from '../shared/ui';

@Component({
  selector: 'vx-not-found-page',
  imports: [RouterLink, LucideAngularModule, Button, Card, EmptyState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="sr-only">Page not found</h1>
    <vx-card padding="none" class="mx-auto mt-4 max-w-md md:mt-12">
      <vx-empty-state [icon]="icons.inbox" title="404 · Page not found" message="This page does not exist or was moved.">
        <a vxButton routerLink="/crash" variant="primary" size="sm">
          <lucide-icon [img]="icons.rocket" [size]="14" aria-hidden="true" />Back to the game
        </a>
      </vx-empty-state>
    </vx-card>
  `
})
export class NotFoundPage {
  protected readonly icons = Icons;
}
