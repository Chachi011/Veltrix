import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'vx-not-found-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-narrow center">
      <h1>404</h1>
      <p class="muted">This page does not exist.</p>
      <a class="btn btn-primary" routerLink="/crash">Back to the game</a>
    </div>
  `,
  styles: `
    .center { text-align: center; }
    h1 { font-size: 4rem; margin-bottom: 0; }
  `
})
export class NotFoundPage {}
