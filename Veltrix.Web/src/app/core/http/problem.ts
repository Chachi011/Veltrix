import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';
import { mark } from '../i18n/i18n';
import { ProblemDetails } from '../models/api.models';

/** Turns any HTTP error into a ProblemDetails object (the API always sends one; the network may not). */
export function toProblem(error: unknown): ProblemDetails {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return { status: 0, title: mark('Network error'), detail: mark('Cannot reach the server. Check that the API is running.') };
    }
    if (error.error && typeof error.error === 'object') {
      return { status: error.status, ...(error.error as ProblemDetails) };
    }
    return { status: error.status, title: error.statusText, detail: error.message };
  }
  return { title: mark('Unexpected error'), detail: String(error) };
}

/** One human-readable sentence for a toast. */
export function problemMessage(problem: ProblemDetails): string {
  const firstFieldError = problem.errors ? Object.values(problem.errors).flat()[0] : undefined;
  return problem.detail ?? firstFieldError ?? problem.title ?? mark('Something went wrong.');
}

/**
 * Shows server-side validation errors next to the matching form fields.
 * The API returns camelCase field names, which are also our form control names.
 */
export function applyServerErrors(form: FormGroup, error: unknown): void {
  const problem = toProblem(error);
  if (!problem.errors) {
    return;
  }

  for (const [field, messages] of Object.entries(problem.errors)) {
    const control: AbstractControl | null = form.get(field);
    if (control) {
      control.setErrors({ ...(control.errors ?? {}), server: messages[0] });
      control.markAsTouched();
    }
  }
}
