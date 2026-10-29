// Turns the JSON snapshots of an audit entry into one readable line, e.g. "Role: Player → Operator".

import { mark } from '../../core/i18n/i18n';

const MAX_VALUE_LENGTH = 60;

/** "2026-11-24T16:10:35.6847734Z" (the server's ISO format, UTC). */
const ISO_DATE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?Z$/;

/** Translates an English text (I18n.t); the identity function keeps English. */
export type Translate = (english: string) => string;

export interface AuditFormatOptions {
  translate?: Translate;
  /** For free text that may be a known server text (e.g. "Server restarted during the round"); I18n.known. */
  translateText?: Translate;
  /** Locale for dates, e.g. 'ka' or 'en-US'. */
  locale?: string;
}

/** The audit actions (AuditActions.cs) by name: "UserRoleChanged" → "User role changed". */
export const AUDIT_ACTION_LABELS: Record<string, string> = {
  GameCreated: mark('Game created'),
  GameUpdated: mark('Game updated'),
  GameStatusChanged: mark('Game status changed'),
  RoundCancelled: mark('Round cancelled'),
  UserRoleChanged: mark('User role changed'),
  UserBlocked: mark('User blocked'),
  UserUnblocked: mark('User unblocked'),
  BalanceAdjusted: mark('Balance adjusted'),
  SettingChanged: mark('Setting changed'),
  LimitsChanged: mark('Limits changed'),
  SelfExclusionStarted: mark('Self exclusion started')
};

/** Entity types of the audit log (AuditLog.EntityType). */
export const AUDIT_ENTITY_LABELS: Record<string, string> = {
  User: mark('User'),
  Game: mark('Game'),
  GameRound: mark('Round'),
  SystemSetting: mark('Setting')
};

/** Field names that appear in the snapshots (humanized), listed so the i18n check knows them. */
export const AUDIT_FIELD_NAMES = [
  mark('Role'),
  mark('Is blocked'),
  mark('Reason'),
  mark('Balance'),
  mark('Amount'),
  mark('Period'),
  mark('Ends at'),
  mark('State'),
  mark('Refunded bets count'),
  mark('Refunded total'),
  mark('Status'),
  mark('Value'),
  mark('Name'),
  mark('Slug'),
  mark('Description'),
  mark('Min bet'),
  mark('Max bet'),
  mark('Betting duration sec'),
  mark('Pause between rounds sec'),
  mark('Daily bet limit'),
  mark('Pending daily bet limit'),
  mark('Pending limit applies at'),
  mark('Session reminder min')
];

/** Enum values in the snapshots, shown by their English label: "Days30" → "30 days". */
const VALUE_LABELS: Record<string, string> = {
  Player: mark('Player'),
  Operator: mark('Operator'),
  Admin: mark('Admin'),
  Active: mark('Active'),
  Maintenance: mark('Maintenance'),
  Disabled: mark('Disabled'),
  Waiting: mark('Waiting'),
  BettingOpen: mark('Betting open'),
  BettingClosed: mark('Betting closed'),
  Running: mark('Running'),
  Crashed: mark('Crashed'),
  Finished: mark('Finished'),
  Cancelled: mark('Cancelled'),
  Hours24: mark('24 hours'),
  Days7: mark('7 days'),
  Days30: mark('30 days')
};

/** "isBlocked" → "Is blocked", "refundedBetsCount" → "Refunded bets count". */
export function humanizeKey(key: string): string {
  const words = key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

/** "UserRoleChanged" → "User role changed" (translated when a translator is given). */
export function actionLabel(action: string, translate: Translate = (s) => s): string {
  return translate(AUDIT_ACTION_LABELS[action] ?? humanizeKey(action));
}

function formatValue(value: unknown, options: AuditFormatOptions): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }
  if (typeof value === 'string' && ISO_DATE.test(value)) {
    const date = new Intl.DateTimeFormat(options.locale ?? 'en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      timeZone: 'UTC'
    });
    return `${date.format(new Date(value))} UTC`;
  }
  if (options.translate) {
    if (typeof value === 'boolean') {
      return options.translate(value ? 'Yes' : 'No');
    }
    if (typeof value === 'string' && VALUE_LABELS[value]) {
      return options.translate(VALUE_LABELS[value]);
    }
  }
  if (typeof value === 'string' && options.translateText) {
    value = options.translateText(value);
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
 * is shown as "Reason: Event prize". Empty string when nothing differs. With a translator, field names,
 * yes/no and enum values are shown in the current language (free text such as a reason stays as typed).
 */
export function describeChange(oldJson: string | null, newJson: string | null, options: AuditFormatOptions = {}): string {
  const before = parse(oldJson);
  const after = parse(newJson);
  const keys = [...new Set([...Object.keys(before), ...Object.keys(after)])];
  const name = (key: string) => (options.translate ? options.translate(humanizeKey(key)) : humanizeKey(key));

  return keys
    .filter((key) => JSON.stringify(before[key]) !== JSON.stringify(after[key]))
    .map((key) =>
      key in before && key in after
        ? `${name(key)}: ${formatValue(before[key], options)} → ${formatValue(after[key], options)}`
        : `${name(key)}: ${formatValue(key in after ? after[key] : before[key], options)}`
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
