import { describeChange, humanizeKey, prettyJson } from './audit-format';

describe('audit log formatting', () => {
  it('shows a role change as old → new', () => {
    expect(describeChange('{"role":"Player"}', '{"role":"Operator"}')).toBe('Role: Player → Operator');
  });

  it('lists only the fields that changed, plus fields that only exist afterwards', () => {
    expect(describeChange('{"isBlocked":false}', '{"isBlocked":true,"reason":"Spam"}')).toBe(
      'Is blocked: false → true · Reason: Spam'
    );
    expect(describeChange('{"balance":100}', '{"balance":150,"amount":50,"reason":"Event prize"}')).toBe(
      'Balance: 100 → 150 · Amount: 50 · Reason: Event prize'
    );
  });

  it('handles a creation (no old value) and an unchanged entry', () => {
    expect(describeChange(null, '{"name":"Crash"}')).toBe('Name: Crash');
    expect(describeChange('{"value":"100"}', '{"value":"100"}')).toBe('');
  });

  it('shows null as a dash and shortens very long values', () => {
    expect(describeChange('{"dailyBetLimit":null}', '{"dailyBetLimit":50}')).toBe('Daily bet limit: — → 50');
    const long = 'x'.repeat(100);
    expect(describeChange(null, `{"reason":"${long}"}`).length).toBeLessThan(80);
  });

  it('shows server timestamps as readable UTC dates', () => {
    expect(describeChange(null, '{"period":"Days30","endsAt":"2026-11-24T16:10:35.6847734Z"}')).toBe(
      'Period: Days30 · Ends at: Nov 24, 2026, 16:10 UTC'
    );
  });

  it('turns camelCase keys into words and pretty-prints JSON', () => {
    expect(humanizeKey('refundedBetsCount')).toBe('Refunded bets count');
    expect(prettyJson('{"a":1}')).toBe('{\n  "a": 1\n}');
    expect(prettyJson('not json')).toBe('not json');
    expect(prettyJson(null)).toBe('—');
  });
});
