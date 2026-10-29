import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { LimitsApi } from '../api/limits.api';
import { WalletApi } from '../api/wallet.api';
import { PlayerLimitsDto, WalletDto } from '../models/api.models';
import { ToastStore } from '../ui/toast.store';
import { DEFAULT_REMINDER_MIN, LimitsStore } from './limits.store';
import { WalletStore } from './wallet.store';

const wallet = (balance: number, overrides: Partial<WalletDto> = {}): WalletDto => ({
  balance,
  dailyBonusAmount: 100,
  canClaimDailyBonus: true,
  lastDailyBonusAt: null,
  nextDailyBonusAt: null,
  ...overrides
});

const limits = (overrides: Partial<PlayerLimitsDto> = {}): PlayerLimitsDto => ({
  dailyBetLimit: null,
  hasPendingLimitChange: false,
  pendingDailyBetLimit: null,
  pendingLimitAppliesAt: null,
  betToday: 0,
  remainingToday: null,
  dayResetsAt: '2026-10-28T00:00:00Z',
  sessionReminderMin: 60,
  isSelfExcluded: false,
  selfExcludedUntil: null,
  limitIncreaseDelayHours: 24,
  ...overrides
});

describe('WalletStore', () => {
  let store: InstanceType<typeof WalletStore>;
  let api: jasmine.SpyObj<WalletApi>;
  let toasts: { success: jasmine.Spy; error: jasmine.Spy; info: jasmine.Spy };

  beforeEach(() => {
    api = jasmine.createSpyObj<WalletApi>('WalletApi', ['get', 'transactions', 'claimDailyBonus']);
    toasts = jasmine.createSpyObj('ToastStore', ['success', 'error', 'info']);
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: WalletApi, useValue: api }, { provide: ToastStore, useValue: toasts }]
    });
    store = TestBed.inject(WalletStore);
  });

  it('load puts the balance in the header', async () => {
    api.get.and.returnValue(of(wallet(1250)));
    await store.load();
    expect(store.balance()).toBe(1250);
  });

  it('claiming the daily bonus updates the balance and says so', async () => {
    api.claimDailyBonus.and.returnValue(of(wallet(1100, { canClaimDailyBonus: false })));
    await store.claimDailyBonus();
    expect(store.balance()).toBe(1100);
    expect(toasts.success).toHaveBeenCalledWith('Daily bonus claimed: +100 VLX');
    expect(store.claiming()).toBeFalse();
  });

  it('a refused claim changes nothing and the button is usable again', async () => {
    api.get.and.returnValue(of(wallet(1000)));
    await store.load();
    api.claimDailyBonus.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400 })));

    await store.claimDailyBonus();

    expect(store.balance()).toBe(1000);
    expect(store.claiming()).toBeFalse();
  });

  it('a live BalanceUpdated sets the balance without a request', () => {
    store.setBalance(987.5);
    expect(store.balance()).toBe(987.5);
    expect(api.get).not.toHaveBeenCalled();
  });

  it('filters keep the other query values (type filter returns to page 1 by the caller)', async () => {
    api.transactions.and.returnValue(of({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 }));
    await store.loadTransactions({ page: 2 });
    await store.loadTransactions({ type: 'Bonus' });
    expect(api.transactions.calls.mostRecent().args[0]).toEqual({ page: 2, pageSize: 10, type: 'Bonus' });
  });

  it('reset forgets the previous user', async () => {
    api.get.and.returnValue(of(wallet(500)));
    await store.load();
    store.reset();
    expect(store.balance()).toBeNull();
    expect(store.wallet()).toBeNull();
  });
});

describe('LimitsStore', () => {
  let store: InstanceType<typeof LimitsStore>;
  let api: jasmine.SpyObj<LimitsApi>;

  beforeEach(() => {
    api = jasmine.createSpyObj<LimitsApi>('LimitsApi', ['get', 'update', 'selfExclude']);
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection(), { provide: LimitsApi, useValue: api }] });
    store = TestBed.inject(LimitsStore);
  });

  it('uses the default reminder (60 min) until the limits are loaded', async () => {
    expect(store.reminderMinutes()).toBe(DEFAULT_REMINDER_MIN);
    api.get.and.returnValue(of(limits({ sessionReminderMin: 30 })));
    await store.load();
    expect(store.reminderMinutes()).toBe(30);
  });

  it('a self-exclusion is reflected for the Crash page (betting paused)', async () => {
    api.selfExclude.and.returnValue(of(limits({ isSelfExcluded: true, selfExcludedUntil: '2026-11-03T12:00:00Z' })));

    expect(await store.selfExclude('Days7')).toBeTrue();

    expect(api.selfExclude).toHaveBeenCalledWith('Days7');
    expect(store.isSelfExcluded()).toBeTrue();
    expect(store.selfExcludedUntil()).toBe('2026-11-03T12:00:00Z');
    expect(store.excluding()).toBeFalse();
  });

  it('a refused update resolves false and keeps the saved values', async () => {
    api.get.and.returnValue(of(limits({ dailyBetLimit: 50 })));
    await store.load();
    api.update.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400 })));

    expect(await store.update({ dailyBetLimit: 0, sessionReminderMin: 60 })).toBeFalse();
    expect(store.limits()?.dailyBetLimit).toBe(50);
    expect(store.saving()).toBeFalse();
  });
});
