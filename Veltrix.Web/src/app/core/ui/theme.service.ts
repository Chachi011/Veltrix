import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

export type Theme = 'dark' | 'light';

const STORAGE_KEY = 'vx.theme';

/**
 * Dark / light theme. The choice is a CSS attribute on <html> (data-theme), and every color
 * in the app is a CSS variable that depends on it, so no component needs theme-specific code.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  /** index.html already applied the saved / system theme before Angular started; read it back. */
  readonly theme = signal<Theme>(
    this.document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark'
  );

  constructor() {
    effect(() => {
      const theme = this.theme();
      this.document.documentElement.setAttribute('data-theme', theme);
      try {
        localStorage.setItem(STORAGE_KEY, theme);
      } catch {
        // Storage can be blocked (private mode); the theme still works for this visit
      }
    });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }
}
