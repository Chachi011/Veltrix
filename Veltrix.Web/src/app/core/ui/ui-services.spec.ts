import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';
import { ToastStore } from './toast.store';

describe('ToastStore', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] }));

  it('shows at most 4 toasts, newest last', () => {
    const toasts = TestBed.inject(ToastStore);
    for (let i = 1; i <= 6; i++) {
      toasts.info(`message ${i}`);
    }
    expect(toasts.toasts().map((t) => t.message)).toEqual(['message 3', 'message 4', 'message 5', 'message 6']);
  });

  it('errors stay longer than success messages (6 s vs 3.5 s)', () => {
    // Zoneless app: Jasmine's own fake clock instead of Angular's fakeAsync
    jasmine.clock().install();
    try {
      const toasts = TestBed.inject(ToastStore);
      toasts.success('Saved.');
      toasts.error('Too late: the round has already crashed.');

      jasmine.clock().tick(3500);
      expect(toasts.toasts().map((t) => t.kind)).toEqual(['error']);
      jasmine.clock().tick(2500);
      expect(toasts.toasts()).toEqual([]);
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('dismiss removes one toast at once', () => {
    const toasts = TestBed.inject(ToastStore);
    toasts.info('a');
    toasts.info('b');
    toasts.dismiss(toasts.toasts()[0].id);
    expect(toasts.toasts().map((t) => t.message)).toEqual(['b']);
  });
});

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
  });

  it('a fixed theme is applied to <html> and remembered', () => {
    const theme = TestBed.inject(ThemeService);
    theme.setMode('light');
    TestBed.tick();

    expect(theme.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('vx.theme')).toBe('light');
  });

  it('"System" forgets the fixed choice and follows the device', () => {
    const theme = TestBed.inject(ThemeService);
    theme.setMode('dark');
    TestBed.tick();
    theme.setMode('system');
    TestBed.tick();

    const device = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    expect(theme.theme()).toBe(device);
    expect(localStorage.getItem('vx.theme')).toBeNull();
  });

  it('toggle switches to the other fixed theme', () => {
    const theme = TestBed.inject(ThemeService);
    theme.setMode('dark');
    theme.toggle();
    expect(theme.mode()).toBe('light');
    theme.toggle();
    expect(theme.mode()).toBe('dark');
  });
});
