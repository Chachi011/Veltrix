import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { PreferencesStore } from '../../core/state/preferences.store';
import { ThemeService } from '../../core/ui/theme.service';
import { Button } from './button';
import { Icons } from './icons';

@Component({
  selector: 'vx-theme-toggle',
  imports: [LucideAngularModule, Button, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      vxButton
      type="button"
      variant="ghost"
      size="icon"
      (click)="preferences.toggleTheme()"
      [attr.aria-label]="theme.theme() === 'dark' ? ('Switch to light theme' | t) : ('Switch to dark theme' | t)"
      [title]="theme.theme() === 'dark' ? ('Light theme' | t) : ('Dark theme' | t)"
    >
      <lucide-icon [img]="theme.theme() === 'dark' ? icons.sun : icons.moon" [size]="18" aria-hidden="true" />
    </button>
  `
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);
  /** Saves the choice on the account when logged in. */
  protected readonly preferences = inject(PreferencesStore);
  protected readonly icons = Icons;
}
