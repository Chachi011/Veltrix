import { HttpClient } from '@angular/common/http';
import { silent } from '../http/http-context';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChangePasswordRequest,
  ProfileDto,
  PublicProfileDto,
  UpdatePreferencesRequest,
  UserPreferencesDto
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProfileApi {
  private readonly http = inject(HttpClient);

  get(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>('/api/profile');
  }

  update(nickname: string): Observable<ProfileDto> {
    return this.http.put<ProfileDto>('/api/profile', { nickname });
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>('/api/profile/password', request);
  }

  /** multipart/form-data with the field name the API expects ("file"). */
  uploadAvatar(file: File): Observable<ProfileDto> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<ProfileDto>('/api/profile/avatar', body);
  }

  getPublic(nickname: string): Observable<PublicProfileDto> {
    return this.http.get<PublicProfileDto>(`/api/players/${encodeURIComponent(nickname)}`);
  }

  /** For the public profile page: a 404 shows a "player not found" state instead of an error toast. */
  getPublicQuietly(nickname: string): Observable<PublicProfileDto> {
    return this.http.get<PublicProfileDto>(`/api/players/${encodeURIComponent(nickname)}`, { context: silent() });
  }

  /** Theme, sound and animation settings saved on the account. */
  preferences(): Observable<UserPreferencesDto> {
    return this.http.get<UserPreferencesDto>('/api/profile/preferences');
  }

  updatePreferences(request: UpdatePreferencesRequest): Observable<UserPreferencesDto> {
    return this.http.put<UserPreferencesDto>('/api/profile/preferences', request);
  }
}
