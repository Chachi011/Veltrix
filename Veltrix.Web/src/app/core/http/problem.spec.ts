import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';
import { applyServerErrors, problemMessage, toProblem } from './problem';

describe('ProblemDetails helpers (server errors in the UI)', () => {
  it('turns any error into a ProblemDetails object', () => {
    const network = toProblem(new HttpErrorResponse({ status: 0 }));
    expect(network.title).toBe('Network error');

    const api = toProblem(
      new HttpErrorResponse({ status: 400, error: { title: 'Request rejected', detail: 'Betting is closed for this round.' } })
    );
    expect(api).toEqual(jasmine.objectContaining({ status: 400, detail: 'Betting is closed for this round.' }));

    const plain = toProblem(new HttpErrorResponse({ status: 502, statusText: 'Bad Gateway', error: '<html>' }));
    expect(plain.status).toBe(502);
    expect(plain.title).toBe('Bad Gateway');

    expect(toProblem(new Error('boom')).title).toBe('Unexpected error');
  });

  it('picks one sentence for a toast: detail, else the first field error, else the title', () => {
    expect(problemMessage({ title: 'Conflict', detail: 'Nickname is taken.' })).toBe('Nickname is taken.');
    expect(problemMessage({ title: 'Validation failed', errors: { email: ['Email is required.'], nickname: ['Too short.'] } })).toBe(
      'Email is required.'
    );
    expect(problemMessage({ title: 'Forbidden' })).toBe('Forbidden');
    expect(problemMessage({})).toBe('Something went wrong.');
  });

  it('shows server validation errors next to the matching form fields', () => {
    const form = new FormGroup({ email: new FormControl('a@b.c'), nickname: new FormControl('ab') });
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Validation failed',
        errors: { nickname: ['Nickname is already taken.', 'Second message'], notAField: ['ignored'] }
      }
    });

    applyServerErrors(form, error);

    expect(form.controls.nickname.errors).toEqual({ server: 'Nickname is already taken.' });
    expect(form.controls.nickname.touched).toBeTrue();
    expect(form.controls.email.errors).toBeNull();
  });
});
