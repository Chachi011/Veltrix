import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ThemeService } from '../../core/ui/theme.service';
import { Button } from './button';
import { Icons } from './icons';

@Component({
  selector: 'vx-theme-toggle',
  imports: [LucideAngularModule, Button],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      vxButton
      type="button"
      variant="ghost"
      size="icon"
      (click)="theme.toggle()"
      [attr.aria-label]="theme.theme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'"
      [title]="theme.theme() === 'dark' ? 'Light theme' : 'Dark theme'"
    >
      <lucide-icon [img]="theme.theme() === 'dark' ? icons.sun : icons.moon" [size]="18" aria-hidden="true" />
    </button>
  `
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);
  protected readonly icons = Icons;
}
