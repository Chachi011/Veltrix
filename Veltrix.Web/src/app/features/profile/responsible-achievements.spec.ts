import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AchievementsApi } from '../../core/api/achievements.api';
import { PagesApi } from '../../core/api/pages.api';
import { AuthStore } from '../../core/auth/auth.store';
import { I18n } from '../../core/i18n/i18n';
import { GameDto, MyAchievementDto, MyAchievementsDto, UserRole } from '../../core/models/api.models';
import { LimitsStore } from '../../core/state/limits.store';
import { GameCard } from '../games/game-card';
import { READ_TIME_MS, RulesPage } from '../rules/rules-page';
import { MyAchievements } from './my-achievements';

const task = (code: string, kind: MyAchievementDto['kind'], unlockedAt: string | null = null): MyAchievementDto => ({
  code,
  title: code,
  description: `${code} description`,
  icon: 'book',
  xpReward: 10,
  vlxReward: 10,
  unlockedBy: 0,
  kind,
  unlockedAt,
  progress: null,
  target: null
});

describe('Achievements and responsible play (client side)', () => {
  describe('profile: my achievements', () => {
    async function render(data: MyAchievementsDto): Promise<HTMLElement> {
      TestBed.configureTestingModule({
        providers: [provideZonelessChangeDetection(), { provide: AchievementsApi, useValue: { mine: () => of(data) } }]
      });
      // English: the Georgian date names load lazily, so a test that shows a date must not depend on test order
      TestBed.inject(I18n).language.set('en');
      const fixture = TestBed.createComponent(MyAchievements);
      await fixture.whenStable();
      return fixture.nativeElement as HTMLElement;
    }

    it('says why tasks that need a bet are missing while the player cannot bet', async () => {
      const el = await render({ items: [task('SET_DAILY_LIMIT', 'Safety'), task('READ_RULES', 'Learn')], playTasksHidden: true });

      expect(el.querySelector('[role="note"]')?.textContent).toContain('hidden while your betting is paused');
      expect(el.querySelectorAll('li').length).toBe(2);
    });

    it('shows no note, and every task with its kind, when the player can bet', async () => {
      const el = await render({
        items: [task('SET_DAILY_LIMIT', 'Safety'), task('READ_RULES', 'Learn'), task('FIRST_BET', 'Play', '2026-10-29T10:00:00Z')],
        playTasksHidden: false
      });

      expect(el.querySelector('[role="note"]')).toBeNull();
      const kinds = Array.from(el.querySelectorAll('vx-badge')).map((b) => b.textContent!.trim());
      expect(kinds).toEqual(['Safety', 'Learning', 'Playing']);
      expect(el.textContent).toContain('1 of 3 unlocked');
    });
  });

  describe('rules page: "read to the end"', () => {
    let markRead: jasmine.Spy;
    let reachEnd: ((visible?: boolean) => void) | undefined;
    const original = window.IntersectionObserver;

    beforeEach(() => {
      reachEnd = undefined;
      markRead = jasmine.createSpy('markRead').and.returnValue(of(undefined));
      // A fake observer: the test decides when the end of the page becomes visible; like a real one, it stays silent after disconnect()
      window.IntersectionObserver = class {
        private connected = true;
        constructor(private readonly callback: IntersectionObserverCallback) {
          reachEnd = (visible = true) => {
            if (this.connected) {
              this.callback([{ isIntersecting: visible } as IntersectionObserverEntry], this as unknown as IntersectionObserver);
            }
          };
        }
        observe(): void {}
        disconnect(): void {
          this.connected = false;
        }
      } as unknown as typeof IntersectionObserver;
    });

    afterEach(() => {
      window.IntersectionObserver = original;
      jasmine.clock().uninstall();
    });

    async function render(role: UserRole | null): Promise<void> {
      TestBed.configureTestingModule({
        providers: [
          provideZonelessChangeDetection(),
          provideRouter([]),
          { provide: AuthStore, useValue: { role: signal(role) } },
          {
            provide: PagesApi,
            useValue: { get: (slug: string) => of({ slug, language: 'en', title: slug, content: 'Text.', updatedAt: '2026-10-29T10:00:00Z' }), markRead }
          }
        ]
      });
      const fixture = TestBed.createComponent(RulesPage);
      await fixture.whenStable();
    }

    it('a player who keeps the end on screen for the reading time tells the server once', async () => {
      await render('Player');
      jasmine.clock().install(); // after rendering: the zoneless scheduler uses real timers

      reachEnd!();
      jasmine.clock().tick(READ_TIME_MS - 1);
      expect(markRead).not.toHaveBeenCalled(); // opening the page is not reading it

      jasmine.clock().tick(1);
      reachEnd!();
      jasmine.clock().tick(READ_TIME_MS);
      expect(markRead).toHaveBeenCalledOnceWith('no-winning-strategy');
    });

    it('scrolling away before the time is up does not count', async () => {
      await render('Player');
      jasmine.clock().install();

      reachEnd!(true);
      jasmine.clock().tick(READ_TIME_MS / 2);
      reachEnd!(false);
      jasmine.clock().tick(READ_TIME_MS);

      expect(markRead).not.toHaveBeenCalled();
    });

    it('guests and staff send nothing', async () => {
      await render(null);
      expect(reachEnd).toBeUndefined();

      TestBed.resetTestingModule();
      await render('Admin');
      expect(markRead).not.toHaveBeenCalled();
    });
  });

  describe('games catalog: no "Play" for a player who cannot bet', () => {
    const game: GameDto = {
      id: 1, name: 'Crash', slug: 'crash', description: null, type: 'Crash', status: 'Active', minBet: 1, maxBet: 10000,
      bettingDurationSec: 10, pauseBetweenRoundsSec: 5, createdAt: '2026-01-01T00:00:00Z', updatedAt: null
    };

    async function buttonText(canPlayNow: boolean): Promise<string> {
      TestBed.configureTestingModule({
        providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: LimitsStore, useValue: { canPlayNow: signal(canPlayNow) } }]
      });
      const fixture = TestBed.createComponent(GameCard);
      fixture.componentRef.setInput('game', game);
      await fixture.whenStable();
      return (fixture.nativeElement as HTMLElement).querySelector('a[vxButton]')!.textContent!.trim();
    }

    it('offers Play when the player can bet', async () => {
      expect(await buttonText(true)).toBe('Play');
    });

    it('offers only Watch while self-excluded or at the daily limit', async () => {
      expect(await buttonText(false)).toBe('Watch');
    });
  });
});
