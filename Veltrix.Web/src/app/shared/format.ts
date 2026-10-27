// Small formatters shared by charts and tables.

const dayLabel = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });
const wholeNumber = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });

/** "2026-10-24" → "Oct 24" (the server's statistics use UTC days). */
export function formatDay(date: string): string {
  return dayLabel.format(new Date(`${date}T00:00:00Z`));
}

/** 1234.56 → "1,235" (chart axes). */
export function formatWhole(value: number): string {
  return wholeNumber.format(value);
}
