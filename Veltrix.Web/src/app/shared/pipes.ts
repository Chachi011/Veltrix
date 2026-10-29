import { formatDate } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n } from '../core/i18n/i18n';

const vlxFormat = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** 1234.5 → "1,234.50 VLX" */
@Pipe({ name: 'vlx' })
export class VlxPipe implements PipeTransform {
  transform(value: number | null | undefined, withUnit = true): string {
    if (value === null || value === undefined) {
      return '—';
    }
    return withUnit ? `${vlxFormat.format(value)} VLX` : vlxFormat.format(value);
  }
}

/** 2.5 → "2.50x" */
@Pipe({ name: 'mult' })
export class MultiplierPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return value === null || value === undefined ? '—' : `${value.toFixed(2)}x`;
  }
}

/** Long hex hash → "1f06f8e3…f5ad" (the full value goes in a title attribute). */
@Pipe({ name: 'shortHash' })
export class ShortHashPipe implements PipeTransform {
  transform(value: string | null | undefined, keep = 8): string {
    if (!value) {
      return '—';
    }
    return value.length <= keep * 2 ? value : `${value.slice(0, keep)}…${value.slice(-4)}`;
  }
}

/**
 * ISO date → "just now", "5 min ago", "3 h ago", "2 d ago", then "Oct 24", in the current language.
 * Impure so it follows a language switch (see TranslatePipe).
 */
@Pipe({ name: 'timeAgo', pure: false })
export class TimeAgoPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(value: string | null | undefined, now = Date.now()): string {
    if (!value) {
      return '—';
    }
    const seconds = Math.max(0, Math.round((now - Date.parse(value)) / 1000));
    if (seconds < 60) {
      return this.i18n.t('just now');
    }
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return this.i18n.t('{count} min ago', { count: minutes });
    }
    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return this.i18n.t('{count} h ago', { count: hours });
    }
    const days = Math.floor(hours / 24);
    return days < 7 ? this.i18n.t('{count} d ago', { count: days }) : formatDate(value, 'MMM d', this.i18n.locale());
  }
}
