import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

export type Theme = 'dark' | 'light';

/** What the user chose: a fixed theme, or "follow the device" (system). */
export type ThemeMode = Theme | 'system';

const STORAGE_KEY = 'vx.theme';

/**
 * Dark / light theme. The choice is a CSS attribute on <html> (data-theme), and every color
 * in the app is a CSS variable that depends on it, so no component needs theme-specific code.
 * "System" follows the device setting, also when it changes while the app is open.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly systemQuery = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: light)') ?? null;
  private readonly systemTheme = signal<Theme>(this.systemQuery?.matches ? 'light' : 'dark');

  /** index.html already applied the saved / system theme before Angular started; read the choice back. */
  readonly mode = signal<ThemeMode>(readSavedMode());

  /** The theme actually shown. */
  readonly theme = computed<Theme>(() => {
    const mode = this.mode();
    return mode === 'system' ? this.systemTheme() : mode;
  });

  constructor() {
    this.systemQuery?.addEventListener?.('change', (e) => this.systemTheme.set(e.matches ? 'light' : 'dark'));

    effect(() => {
      this.document.documentElement.setAttribute('data-theme', this.theme());
      const mode = this.mode();
      try {
        if (mode === 'system') {
          localStorage.removeItem(STORAGE_KEY);
        } else {
          localStorage.setItem(STORAGE_KEY, mode);
        }
      } catch {
        // Storage can be blocked (private mode); the theme still works for this visit
      }
    });
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
  }

  /** The top bar's sun / moon button: switches to the other fixed theme. */
  toggle(): void {
    this.mode.set(this.theme() === 'dark' ? 'light' : 'dark');
  }
}

function readSavedMode(): ThemeMode {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    return saved === 'dark' || saved === 'light' ? saved : 'system';
  } catch {
    return 'system';
  }
}
