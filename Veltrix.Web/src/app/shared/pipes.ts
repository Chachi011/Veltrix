import { Pipe, PipeTransform } from '@angular/core';

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

const shortDate = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' });

/** ISO date → "just now", "5 min ago", "3 h ago", "2 d ago", then "Oct 24". */
@Pipe({ name: 'timeAgo' })
export class TimeAgoPipe implements PipeTransform {
  transform(value: string | null | undefined, now = Date.now()): string {
    if (!value) {
      return '—';
    }
    const seconds = Math.max(0, Math.round((now - Date.parse(value)) / 1000));
    if (seconds < 60) {
      return 'just now';
    }
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return `${minutes} min ago`;
    }
    const hours = Math.floor(minutes / 60);
    if (hours < 24) {
      return `${hours} h ago`;
    }
    const days = Math.floor(hours / 24);
    return days < 7 ? `${days} d ago` : shortDate.format(new Date(value));
  }
}
