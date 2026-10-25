import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App (shell)', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideZonelessChangeDetection(), provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('renders the brand and, for a guest, the log in / register links', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.brand')?.textContent).toContain('VELTRIX');
    expect(element.textContent).toContain('Log in');
    expect(element.textContent).toContain('Register');
    expect(element.querySelector('.balance')).toBeNull();
  });

  it('toggles the theme', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const html = document.documentElement;
    const before = html.getAttribute('data-theme');

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.icon-button')!.click();
    await fixture.whenStable();

    expect(html.getAttribute('data-theme')).not.toBe(before);
  });
});
