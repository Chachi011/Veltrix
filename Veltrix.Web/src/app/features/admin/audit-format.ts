// Turns the JSON snapshots of an audit entry into one readable line, e.g. "Role: Player → Operator".

const MAX_VALUE_LENGTH = 60;

/** "2026-11-24T16:10:35.6847734Z" (the server's ISO format, UTC). */
const ISO_DATE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?Z$/;
const dateFormat = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  timeZone: 'UTC'
});

/** "isBlocked" → "Is blocked", "refundedBetsCount" → "Refunded bets count". */
export function humanizeKey(key: string): string {
  const words = key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }
  if (typeof value === 'string' && ISO_DATE.test(value)) {
    return `${dateFormat.format(new Date(value))} UTC`;
  }
  const text = typeof value === 'object' ? JSON.stringify(value) : String(value);
  return text.length > MAX_VALUE_LENGTH ? `${text.slice(0, MAX_VALUE_LENGTH - 1)}…` : text;
}

function parse(json: string | null): Record<string, unknown> {
  if (!json) {
    return {};
  }
  try {
    const value: unknown = JSON.parse(json);
    return value && typeof value === 'object' && !Array.isArray(value) ? (value as Record<string, unknown>) : { value };
  } catch {
    return { value: json };
  }
}

/**
 * Only what changed: "Role: Player → Operator". A field that only exists after the change (e.g. a reason)
 * is shown as "Reason: Event prize". Empty string when nothing differs.
 */
export function describeChange(oldJson: string | null, newJson: string | null): string {
  const before = parse(oldJson);
  const after = parse(newJson);
  const keys = [...new Set([...Object.keys(before), ...Object.keys(after)])];

  return keys
    .filter((key) => JSON.stringify(before[key]) !== JSON.stringify(after[key]))
    .map((key) =>
      key in before && key in after
        ? `${humanizeKey(key)}: ${formatValue(before[key])} → ${formatValue(after[key])}`
        : `${humanizeKey(key)}: ${formatValue(key in after ? after[key] : before[key])}`
    )
    .join(' · ');
}

/** "{"role":"Player"}" → pretty JSON for the details row (or the raw text if it is not JSON). */
export function prettyJson(json: string | null): string {
  if (!json) {
    return '—';
  }
  try {
    return JSON.stringify(JSON.parse(json), null, 2);
  } catch {
    return json;
  }
}
