import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Same rules as the server's FluentValidation validators. The client checks them for instant
// feedback; the server checks them again because the client can always be bypassed.

export const NICKNAME_PATTERN = /^[A-Za-z0-9_]{3,20}$/;

/** 8+ chars, at least one uppercase, one lowercase and one digit. */
export const strongPassword: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '');
  if (!value) {
    return null; // "required" reports empty values
  }
  const ok = value.length >= 8 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /[0-9]/.test(value);
  return ok ? null : { passwordStrength: true };
};

/** Date of birth (yyyy-MM-dd) at least `years` ago. */
export const minAge =
  (years: number): ValidatorFn =>
  (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }
    const born = new Date(control.value);
    const limit = new Date();
    limit.setFullYear(limit.getFullYear() - years);
    return born <= limit && born.getFullYear() >= 1900 ? null : { adult: true };
  };

/** Form-group validator: two fields must be equal (password + confirmation). */
export const fieldsMatch =
  (field: string, confirmField: string): ValidatorFn =>
  (group: AbstractControl): ValidationErrors | null => {
    const confirm = group.get(confirmField);
    if (!confirm) {
      return null;
    }
    const mismatch = group.get(field)?.value !== confirm.value;
    const { mismatch: _, ...otherErrors } = confirm.errors ?? {};
    confirm.setErrors(mismatch ? { ...otherErrors, mismatch: true } : Object.keys(otherErrors).length ? otherErrors : null);
    return null;
  };

/** At most 2 decimal places (VLX amounts and multipliers). */
export const twoDecimals: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }
  return Math.round(Number(value) * 100) / 100 === Number(value) ? null : { decimals: true };
};
