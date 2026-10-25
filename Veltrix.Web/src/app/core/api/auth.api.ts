import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SILENT_ERRORS, SKIP_LOADING } from '../http/http-context';
import { AuthResponse, LoginRequest, RegisterRequest, ResetPasswordRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/auth';

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/register`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, request);
  }

  /** Background call: no toast, no loading bar (a failure just means "log in again"). */
  refresh(refreshToken: string): Observable<AuthResponse> {
    const context = new HttpContext().set(SILENT_ERRORS, true).set(SKIP_LOADING, true);
    return this.http.post<AuthResponse>(`${this.base}/refresh`, { refreshToken }, { context });
  }

  logout(refreshToken: string): Observable<void> {
    const context = new HttpContext().set(SILENT_ERRORS, true);
    return this.http.post<void>(`${this.base}/logout`, { refreshToken }, { context });
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/forgot-password`, { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, request);
  }
}
