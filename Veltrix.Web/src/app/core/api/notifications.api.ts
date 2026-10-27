import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_LOADING, toParams } from '../http/http-context';
import { NotificationPageDto } from '../models/api.models';

/** The bell. Background requests: they never show the top loading bar. */
@Injectable({ providedIn: 'root' })
export class NotificationsApi {
  private readonly http = inject(HttpClient);
  private readonly background = () => new HttpContext().set(SKIP_LOADING, true);

  list(page: number, pageSize: number): Observable<NotificationPageDto> {
    return this.http.get<NotificationPageDto>('/api/notifications', {
      params: toParams({ page, pageSize }),
      context: this.background()
    });
  }

  markRead(id: number): Observable<void> {
    return this.http.put<void>(`/api/notifications/${id}/read`, null, { context: this.background() });
  }

  markAllRead(): Observable<{ updated: number }> {
    return this.http.put<{ updated: number }>('/api/notifications/read-all', null, { context: this.background() });
  }
}
