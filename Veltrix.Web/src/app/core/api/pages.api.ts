import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { silent } from '../http/http-context';
import { StaticPageDto } from '../models/api.models';

/** Content pages from the StaticPages table (rules, why there is no winning strategy). Reading is public. */
@Injectable({ providedIn: 'root' })
export class PagesApi {
  private readonly http = inject(HttpClient);

  get(slug: string): Observable<StaticPageDto> {
    return this.http.get<StaticPageDto>(`/api/pages/${encodeURIComponent(slug)}`);
  }

  /** A player read the page to its end; the server decides whether that completes "Know the rules". Silent: never an error toast. */
  markRead(slug: string): Observable<void> {
    return this.http.post<void>(`/api/pages/${encodeURIComponent(slug)}/read`, null, { context: silent() });
  }
}
