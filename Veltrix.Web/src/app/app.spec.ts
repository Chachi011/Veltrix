import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App (shell)', () => {
  beforeEach(async () => {
    localStorage.removeItem('vx.sidebarCollapsed');
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('renders sidebar, top bar and bottom nav; a guest sees log in / register and no balance', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('vx-sidebar aside')?.textContent).toContain('VELTRIX');
    expect(element.querySelector('vx-top-bar')?.textContent).toContain('Log in');
    expect(element.querySelector('vx-top-bar')?.textContent).toContain('Register');
    expect(element.querySelector('vx-bottom-nav nav')).not.toBeNull();
    expect(element.querySelector('vx-animated-number')).toBeNull(); // the balance chip is for logged-in users
  });

  it('guests do not see History and Wallet in the navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const nav = (fixture.nativeElement as HTMLElement).querySelector('vx-sidebar nav')!.textContent ?? '';

    expect(nav).toContain('Play');
    expect(nav).not.toContain('Wallet');
    expect(nav).not.toContain('History');
  });

  it('collapses the sidebar and remembers it', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('[aria-label="Collapse sidebar"]')!.click();
    await fixture.whenStable();

    expect(element.querySelector('vx-sidebar aside')?.className).toContain('w-[72px]');
    expect(localStorage.getItem('vx.sidebarCollapsed')).toBe('1');
    localStorage.removeItem('vx.sidebarCollapsed');
  });

  it('toggles the theme', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const html = document.documentElement;
    const before = html.getAttribute('data-theme');

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('vx-theme-toggle button')!.click();
    await fixture.whenStable();

    expect(html.getAttribute('data-theme')).not.toBe(before);
  });
});
