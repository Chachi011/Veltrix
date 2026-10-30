import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AchievementDto, MyAchievementsDto } from '../models/api.models';

/** Achievements (spec 4.6). The server unlocks them (round settlement, verify, rules, limits); the client only reads them. */
@Injectable({ providedIn: 'root' })
export class AchievementsApi {
  private readonly http = inject(HttpClient);

  /** The active catalog (safety and learning first) with rewards and how many players unlocked each (open to guests). */
  all(): Observable<AchievementDto[]> {
    return this.http.get<AchievementDto[]>('/api/achievements');
  }

  /** The logged-in player's achievements; play tasks are left out while the player cannot bet (responsible play). */
  mine(): Observable<MyAchievementsDto> {
    return this.http.get<MyAchievementsDto>('/api/achievements/my');
  }
}
