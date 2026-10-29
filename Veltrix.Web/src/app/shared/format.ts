// Small formatters shared by charts and tables.

const wholeNumber = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });

/** "2026-10-24" → "Oct 24" / "24 ოქტ." (the server's statistics use UTC days). Pass I18n.locale(). */
export function formatDay(date: string, locale = 'en-US'): string {
  return new Intl.DateTimeFormat(locale, { month: 'short', day: 'numeric', timeZone: 'UTC' }).format(new Date(`${date}T00:00:00Z`));
}

/** 1234.56 → "1,235" (chart axes). */
export function formatWhole(value: number): string {
  return wholeNumber.format(value);
}
