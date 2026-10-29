import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AchievementDto, MyAchievementDto } from '../models/api.models';

/** Achievements (spec 4.6). The server unlocks them when a round is settled; the client only reads them. */
@Injectable({ providedIn: 'root' })
export class AchievementsApi {
  private readonly http = inject(HttpClient);

  /** The whole catalog with rewards and how many players unlocked each (open to guests). */
  all(): Observable<AchievementDto[]> {
    return this.http.get<AchievementDto[]>('/api/achievements');
  }

  /** The logged-in player's achievements: unlocked ones with the date, the others with their progress. */
  mine(): Observable<MyAchievementDto[]> {
    return this.http.get<MyAchievementDto[]>('/api/achievements/my');
  }
}
