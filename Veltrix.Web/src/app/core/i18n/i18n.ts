import { DOCUMENT, registerLocaleData } from '@angular/common';
import { Injectable, inject, isDevMode, signal } from '@angular/core';

/*
 * Georgian and English (spec 4.7). How it works, in short:
 * - The code is written in English, and the English text IS the key: {{ 'Place bet' | t }}.
 * - ALL Georgian text lives in ONE file, src/i18n/ka.json (sections "ui", "server", "pages").
 * - A missing translation shows the English text, never a broken key.
 * - The Georgian file and the Georgian date names are downloaded only when Georgian is used,
 *   as their own lazy chunk (so English visitors never load them).
 * - Switching the language needs no page reload: the language is a signal, and everything
 *   that shows text reads it.
 */

export type Language = 'ka' | 'en';

/** Georgian is the default (spec 4.7: "ქართული (ნაგულისხმევი) და ინგლისური"). */
export const DEFAULT_LANGUAGE: Language = 'ka';

export const LANGUAGES: readonly Language[] = ['ka', 'en'];

export type TranslationParams = Record<string, string | number>;

/** Shape of src/i18n/ka.json. */
export interface GeorgianFile {
  /** English UI text → Georgian. */
  ui: Record<string, string>;
  /** English messages sent by the API (errors, notifications) → Georgian; `{name}` marks a changing value. */
  server: Record<string, string>;
  /** Content pages by slug (the Rules page). */
  pages: Record<string, { title: string; content: string }>;
}

/**
 * Marks a string as translatable without translating it yet, for text defined in constants (nav items,
 * tab labels) that is translated where it is shown: `label: mark('Home')` … `{{ item.label | t }}`.
 * It only returns its argument; the i18n check (npm run i18n:check) uses it to find the text.
 */
export const mark = (text: string): string => text;

/** "Bet {amount} VLX" + { amount: 10 } → "Bet 10 VLX". Unknown names are left as they are. */
export function interpolate(text: string, params?: TranslationParams): string {
  if (!params) {
    return text;
  }
  return text.replace(/\{(\w+)\}/g, (whole, name: string) => (name in params ? String(params[name]) : whole));
}

interface ServerPattern {
  regex: RegExp;
  names: string[];
  georgian: string;
}

/** "Bet must be between {min} and {max} VLX." → a regex that captures min and max. */
export function toPattern(english: string, georgian: string): ServerPattern | null {
  const names: string[] = [];
  const parts = english.split(/\{(\w+)\}/g);
  if (parts.length === 1) {
    return null; // no placeholder: matched exactly instead
  }
  const source = parts
    .map((part, i) => {
      if (i % 2 === 1) {
        names.push(part);
        return '(.+?)';
      }
      return part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    })
    .join('');
  return { regex: new RegExp(`^${source}$`), names, georgian };
}

@Injectable({ providedIn: 'root' })
export class I18n {
  private readonly document = inject(DOCUMENT);

  /** The language on screen. Templates read it through the `t` and `vxDate` pipes. */
  readonly language = signal<Language>(DEFAULT_LANGUAGE);

  private readonly georgian = signal<GeorgianFile | null>(null);
  private serverPatterns: ServerPattern[] = [];
  private loading: Promise<void> | null = null;
  private readonly reportedMissing = new Set<string>();

  /** Switches the language, after loading what it needs (the first switch to Georgian downloads ka.json). */
  async use(language: Language): Promise<void> {
    if (language === 'ka') {
      try {
        await this.loadGeorgian();
      } catch {
        // Offline before the file was cached: stay readable in English rather than show nothing
        language = 'en';
      }
    }
    this.language.set(language);
    this.document.documentElement.lang = language;
  }

  /** The text in the current language (English text in, English or Georgian out). */
  t(english: string, params?: TranslationParams): string {
    if (this.language() !== 'ka') {
      return interpolate(english, params);
    }
    const georgian = this.georgian()?.ui[english];
    if (georgian === undefined) {
      this.reportMissing('ui', english);
    }
    return interpolate(georgian ?? english, params);
  }

  /** A message from the API (error detail, field error, notification): exact match first, then the patterns. */
  server(message: string | null | undefined): string {
    if (!message || this.language() !== 'ka') {
      return message ?? '';
    }
    const georgian = this.lookup(message);
    if (georgian === undefined) {
      this.reportMissing('server', message);
    }
    return georgian ?? message;
  }

  /**
   * Free text that MAY be a known server text (e.g. an audit reason: the engine writes "Server restarted during
   * the round", an admin types anything). Translated when known, otherwise shown as typed, without a warning.
   */
  known(text: string): string {
    return this.language() === 'ka' ? (this.lookup(text) ?? text) : text;
  }

  /** Georgian for an English server text, or undefined. */
  private lookup(message: string): string | undefined {
    const file = this.georgian();
    const exact = file?.server[message];
    if (exact !== undefined) {
      return exact;
    }
    for (const pattern of this.serverPatterns) {
      const match = pattern.regex.exec(message);
      if (match) {
        // A captured value that is itself a known word ("Player", "Operator") is translated too
        const values = Object.fromEntries(pattern.names.map((name, i) => [name, file?.ui[match[i + 1]] ?? match[i + 1]]));
        return interpolate(pattern.georgian, values);
      }
    }
    // Messages the client itself puts in a ProblemDetails (e.g. "Cannot reach the server") are UI texts
    return file?.ui[message];
  }

  /** A content page in the current language, or null to use the server's (English) version. */
  page(slug: string): { title: string; content: string } | null {
    return this.language() === 'ka' ? (this.georgian()?.pages[slug] ?? null) : null;
  }

  /** Locale id for Angular's formatDate / formatNumber. */
  locale(): string {
    return this.language() === 'ka' ? 'ka' : 'en-US';
  }

  private loadGeorgian(): Promise<void> {
    this.loading ??= Promise.all([import('../../../i18n/ka.json'), import('@angular/common/locales/ka')])
      .then(([file, locale]) => {
        registerLocaleData(locale.default, 'ka');
        const georgian = file.default as GeorgianFile;
        this.serverPatterns = Object.entries(georgian.server)
          .map(([english, ka]) => toPattern(english, ka))
          .filter((p): p is ServerPattern => p !== null);
        this.georgian.set(georgian);
      })
      .catch((error: unknown) => {
        this.loading = null; // try again next time
        throw error;
      });
    return this.loading;
  }

  /** Development only: one console warning per text that has no Georgian version yet. */
  private reportMissing(section: string, english: string): void {
    if (isDevMode() && this.georgian() && !this.reportedMissing.has(english)) {
      this.reportedMissing.add(english);
      console.warn(`[i18n] No Georgian for ${section}: "${english}"`);
    }
  }
}
