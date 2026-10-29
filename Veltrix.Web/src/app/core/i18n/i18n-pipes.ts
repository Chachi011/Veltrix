import { formatDate } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n, TranslationParams } from './i18n';

/**
 * {{ 'Place bet' | t }} · {{ 'Bet {amount} VLX' | t: { amount: 10 } }}
 *
 * Impure on purpose: a pure pipe is only re-run when its input changes, so it would keep showing the old
 * language after a switch. This one runs with its view and reads the language signal, so a switch
 * re-renders every text at once. The work is one dictionary lookup.
 */
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(english: string | null | undefined, params?: TranslationParams): string {
    return english ? this.i18n.t(english, params) : '';
  }
}

/**
 * Translates the `label` of every item (tabs, selects): <vx-tabs [items]="ranges | tLabels" … />.
 * The labels are marked with mark() where the list is defined. Returns the same array until the
 * list or the language changes, so the receiving component is not re-rendered on every check.
 */
@Pipe({ name: 'tLabels', pure: false })
export class TranslateLabelsPipe implements PipeTransform {
  private readonly i18n = inject(I18n);
  private lastItems: readonly { label: string }[] | null = null;
  private lastLanguage = '';
  private lastResult: { label: string }[] = [];

  transform<T extends { label: string }>(items: readonly T[]): T[] {
    const language = this.i18n.language();
    if (items !== this.lastItems || language !== this.lastLanguage) {
      this.lastItems = items;
      this.lastLanguage = language;
      this.lastResult = items.map((item) => ({ ...item, label: this.i18n.t(item.label) }));
    }
    return this.lastResult as T[];
  }
}

/** The two halves of a sentence around its highlighted value. */
export interface SplitText {
  before: string;
  after: string;
}

const SLOT = '\u0000';

/**
 * A translated sentence with ONE value shown in its own element (e.g. a mono number):
 *   @let s = 'Cashed out at {value}' | tSplit;
 *   {{ s.before }}<span class="num">{{ x | mult }}</span>{{ s.after }}
 * The whole sentence is translated, so each language keeps its own word order
 * (Georgian puts suffixes after the number: "2.00x-ზე").
 */
@Pipe({ name: 'tSplit', pure: false })
export class TranslateSplitPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(english: string, params?: TranslationParams): SplitText {
    const [before, after = ''] = this.i18n.t(english, { ...params, value: SLOT }).split(SLOT);
    return { before, after };
  }
}

/** A message that came from the API (error, notification): {{ notification.title | serverText }}. */
@Pipe({ name: 'serverText', pure: false })
export class ServerTextPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(message: string | null | undefined): string {
    return this.i18n.server(message);
  }
}

/**
 * Free text that may be a known server text, e.g. a ledger reason ("Bet on round #12" from the server, or a reason
 * an admin typed) or a game description: translated when known, otherwise shown as typed (no missing-text warning).
 */
@Pipe({ name: 'knownText', pure: false })
export class KnownTextPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(text: string | null | undefined): string {
    return text ? this.i18n.known(text) : '';
  }
}

/**
 * Angular's date pipe in the current language (Georgian month names in Georgian):
 * {{ bet.placedAt | vxDate: 'medium' }}. Impure for the same reason as `t`.
 */
@Pipe({ name: 'vxDate', pure: false })
export class LocalDatePipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(value: string | number | Date | null | undefined, format = 'mediumDate', timezone?: string): string {
    if (value === null || value === undefined || value === '') {
      return '—';
    }
    return formatDate(value, format, this.i18n.locale(), timezone);
  }
}
