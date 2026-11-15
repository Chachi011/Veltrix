import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { AuthStore } from '../../core/auth/auth.store';
import { I18n } from '../../core/i18n/i18n';
import { AdminUserDetailsDto } from '../../core/models/api.models';
import { ModalService } from '../../shared/ui/modal';
import { UserDrawer } from './user-drawer';

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;

const nova: AdminUserDetailsDto = {
  id: 11, email: 'nova@demo.veltrix.dev', nickname: 'nova', avatarUrl: null, role: 'Player', dateOfBirth: '2000-01-01',
  isBlocked: false, isEmailVerified: false, failedLoginCount: 0, lockoutUntil: null, createdAt: '2026-09-23T10:00:00Z',
  lastLoginAt: null, activeSessions: 0, balance: 100, totalBets: 0, cashOutCount: 0, totalWagered: 0, totalPayout: 0,
  biggestMultiplier: 0, dailyBetLimit: null, selfExcludedUntil: null, recentTransactions: [], recentAdjustments: []
};

async function settle(): Promise<void> {
  for (let i = 0; i < 3; i++) {
    await new Promise((resolve) => setTimeout(resolve));
    await TestBed.inject(ApplicationRef).whenStable();
  }
}

function typeInto(selector: string, value: string): void {
  const input = document.querySelector<HTMLInputElement | HTMLTextAreaElement>(`.vx-drawer-panel ${selector}`)!;
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
}

describe('UserDrawer: balance adjustment', () => {
  let api: jasmine.SpyObj<AdminApi>;

  beforeEach(() => {
    api = jasmine.createSpyObj<AdminApi>('AdminApi', ['user', 'adjustBalance']);
    api.user.and.returnValue(of(nova));
    api.adjustBalance.and.returnValue(
      of({ id: 1, userId: 11, amount: 50, reason: 'Welcome gift', adminNickname: 'admin', balanceBefore: 100, balanceAfter: 150, createdAt: '2026-11-15T12:00:00Z' })
    );
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AdminApi, useValue: api },
        { provide: AuthStore, useValue: { user: signal({ id: 1, nickname: 'admin' }) } }
      ]
    });
    TestBed.inject(I18n).language.set('en'); // the dates in the drawer need no Georgian locale data then
  });

  it('sends a real UUID as the request id on a plain-HTTP site too (no crypto.randomUUID there)', async () => {
    // Browsers expose crypto.randomUUID only on HTTPS and localhost; the server reads the request id as a Guid
    const randomUUID = Object.getOwnPropertyDescriptor(Crypto.prototype, 'randomUUID')!;
    delete (Crypto.prototype as Partial<Crypto>).randomUUID;
    try {
      const modal = TestBed.inject(ModalService);
      spyOn(modal, 'confirm').and.resolveTo(true);
      const drawer = modal.openDrawer(UserDrawer, { data: 11, ariaLabel: 'nova' });
      await settle();

      typeInto('#adjust-amount', '50');
      typeInto('#adjust-reason', 'Welcome gift');
      document.querySelector<HTMLButtonElement>('.vx-drawer-panel form button[type=submit]')!.click();
      await settle();

      expect(api.adjustBalance).toHaveBeenCalledTimes(1);
      const [userId, request] = api.adjustBalance.calls.mostRecent().args;
      expect(userId).toBe(11);
      expect(request.amount).toBe(50);
      expect(request.requestId).toMatch(GUID);
      drawer.close();
    } finally {
      Object.defineProperty(Crypto.prototype, 'randomUUID', randomUUID);
    }
  });
});
