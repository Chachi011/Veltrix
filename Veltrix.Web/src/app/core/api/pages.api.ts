import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { StaticPageDto } from '../models/api.models';

/** Content pages from the StaticPages table (rules, why there is no winning strategy). Public. */
@Injectable({ providedIn: 'root' })
export class PagesApi {
  private readonly http = inject(HttpClient);

  get(slug: string): Observable<StaticPageDto> {
    return this.http.get<StaticPageDto>(`/api/pages/${encodeURIComponent(slug)}`);
  }
}
