import { FormControl, FormGroup } from '@angular/forms';
import { NICKNAME_PATTERN, fieldsMatch, minAge, strongPassword, twoDecimals } from './validators';

/** yyyy-MM-dd for "today minus this many years (and days)". */
function yearsAgo(years: number, extraDays = 0): string {
  const date = new Date();
  date.setFullYear(date.getFullYear() - years);
  date.setDate(date.getDate() - extraDays);
  return date.toISOString().slice(0, 10);
}

describe('form validators (same rules as the server)', () => {
  it('nickname: 3-20 letters, digits or underscore', () => {
    expect(NICKNAME_PATTERN.test('player_1')).toBeTrue();
    expect(NICKNAME_PATTERN.test('ab')).toBeFalse();
    expect(NICKNAME_PATTERN.test('has space')).toBeFalse();
    expect(NICKNAME_PATTERN.test('a'.repeat(21))).toBeFalse();
  });

  it('strong password: 8+ characters with upper, lower and digit', () => {
    const check = (value: string) => strongPassword(new FormControl(value));
    expect(check('Player123')).toBeNull();
    expect(check('')).toBeNull(); // "required" reports empty values
    expect(check('player123')).toEqual({ passwordStrength: true });
    expect(check('PLAYER123')).toEqual({ passwordStrength: true });
    expect(check('Player')).toEqual({ passwordStrength: true });
  });

  it('minimum age: 18 years or more, born 1900 or later', () => {
    const adult = minAge(18);
    expect(adult(new FormControl(yearsAgo(18, 1)))).toBeNull();
    expect(adult(new FormControl(yearsAgo(17)))).toEqual({ adult: true });
    expect(adult(new FormControl('1899-12-31'))).toEqual({ adult: true });
    expect(adult(new FormControl(''))).toBeNull();
  });

  it('password confirmation must match, without removing other errors of that field', () => {
    const form = new FormGroup(
      { password: new FormControl('Secret123'), confirm: new FormControl('Secret12') },
      { validators: fieldsMatch('password', 'confirm') }
    );
    expect(form.controls.confirm.errors).toEqual({ mismatch: true });

    form.controls.confirm.setValue('Secret123');
    expect(form.controls.confirm.errors).toBeNull();

    form.controls.confirm.setErrors({ server: 'Something else' });
    form.controls.password.setValue('Other123');
    expect(form.controls.confirm.errors).toEqual({ server: 'Something else', mismatch: true });
  });

  it('amounts: at most 2 decimal places', () => {
    const check = (value: unknown) => twoDecimals(new FormControl(value));
    expect(check(10)).toBeNull();
    expect(check(10.25)).toBeNull();
    expect(check('')).toBeNull();
    expect(check(10.255)).toEqual({ decimals: true });
  });
});
