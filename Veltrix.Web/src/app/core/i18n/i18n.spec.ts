import { ChangeDetectionStrategy, Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import ka from '../../../i18n/ka.json';
import { GeorgianFile, I18n, interpolate, toPattern } from './i18n';
import { TranslatePipe, TranslateSplitPipe } from './i18n-pipes';

const georgian = ka as GeorgianFile;

/** "{amount}" and "{date}" in a text, sorted. */
const placeholders = (text: string) => (text.match(/\{\w+\}/g) ?? []).sort();

@Component({
  imports: [TranslatePipe, TranslateSplitPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let won = 'Cashed out at {value}' | tSplit;
    <p id="plain">{{ 'Place bet' | t }}</p>
    <p id="param">{{ 'Round #{number}' | t: { number: 7 } }}</p>
    <p id="split">{{ won.before }}<b>2.00x</b>{{ won.after }}</p>
  `
})
class Sample {}

describe('I18n (Georgian and English)', () => {
  let i18n: I18n;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    i18n = TestBed.inject(I18n);
  });

  afterEach(() => (document.documentElement.lang = 'en'));

  it('shows English as written, and the Georgian version after switching', async () => {
    await i18n.use('en');
    expect(i18n.t('Place bet')).toBe('Place bet');

    await i18n.use('ka');
    expect(i18n.t('Place bet')).toBe('ფსონის დადება');
    expect(i18n.t('Bet placed: {amount} VLX', { amount: '10.00' })).toBe('ფსონი დადებულია: 10.00 VLX');
    expect(document.documentElement.lang).toBe('ka');
  });

  it('falls back to the English text when a Georgian version is missing (never a broken key)', async () => {
    await i18n.use('ka');
    spyOn(console, 'warn'); // the development warning for the missing text
    expect(i18n.t('A text nobody translated')).toBe('A text nobody translated');
  });

  it('translates API messages: exact ones, and ones with changing values (patterns)', async () => {
    await i18n.use('ka');
    expect(i18n.server('Betting is closed for this round.')).toBe('ამ რაუნდზე ფსონების მიღება დასრულებულია.');
    expect(i18n.server('Bet must be between 1.00 and 10000.00 VLX.')).toBe('ფსონი უნდა იყოს 1.00-დან 10000.00 VLX-მდე.');
    // A captured value that is a known word is translated too (the role names)
    expect(i18n.server('An administrator changed your role from Player to Operator.')).toBe(
      'ადმინისტრატორმა შენი როლი შეცვალა: მოთამაშე → ოპერატორი.'
    );
    spyOn(console, 'warn');
    expect(i18n.server('Some brand-new server message.')).toBe('Some brand-new server message.');

    await i18n.use('en');
    expect(i18n.server('Betting is closed for this round.')).toBe('Betting is closed for this round.');
  });

  it('has Georgian content pages only in Georgian (English comes from the database)', async () => {
    await i18n.use('ka');
    expect(i18n.page('rules')?.title).toBe('Crash-ის წესები');
    await i18n.use('en');
    expect(i18n.page('rules')).toBeNull();
  });

  it('re-renders every text when the language changes, without a reload', async () => {
    await i18n.use('en');
    const fixture = TestBed.createComponent(Sample);
    await fixture.whenStable();
    const text = (id: string) => fixture.nativeElement.querySelector(`#${id}`).textContent;
    expect(text('plain')).toBe('Place bet');
    expect(text('param')).toBe('Round #7');
    expect(text('split')).toBe('Cashed out at 2.00x');

    await i18n.use('ka');
    await fixture.whenStable();
    expect(text('plain')).toBe('ფსონის დადება');
    expect(text('param')).toBe('რაუნდი #7');
    // Georgian puts the suffix after the number: the sentence keeps its own word order
    expect(text('split')).toBe('Cash out 2.00x-ზე');
  });

  it('builds patterns that treat the English text literally', () => {
    const pattern = toPattern('Round #{number} (x{value}).', 'რაუნდი #{number} ({value})')!;
    expect(pattern.regex.test('Round #12 (x2.5).')).toBeTrue();
    expect(pattern.regex.test('Round #12 x2.5.')).toBeFalse();
    expect(toPattern('No placeholders.', 'x')).toBeNull();
    expect(interpolate('{a} and {b}', { a: 1 })).toBe('1 and {b}');
  });
});

describe('ka.json (the one Georgian file)', () => {
  const sections = { ui: georgian.ui, server: georgian.server };

  it('has no empty translations', () => {
    for (const [name, entries] of Object.entries(sections)) {
      for (const [english, text] of Object.entries(entries)) {
        expect(text.trim()).withContext(`${name}: "${english}"`).not.toBe('');
      }
    }
  });

  it('uses exactly the same {placeholders} as the English text', () => {
    for (const [name, entries] of Object.entries(sections)) {
      for (const [english, text] of Object.entries(entries)) {
        expect(placeholders(text)).withContext(`${name}: "${english}"`).toEqual(placeholders(english));
      }
    }
  });

  it('keeps the terms Crash, Cash out and VLX as they are', () => {
    for (const [name, entries] of Object.entries(sections)) {
      for (const [english, text] of Object.entries(entries)) {
        for (const term of ['Crash', 'Cash out', 'VLX']) {
          if (english.includes(term)) {
            expect(text).withContext(`${name}: "${english}"`).toContain(term);
          }
        }
      }
    }
  });

  it('keeps the HTML of the fairness steps (only <code> parts are allowed)', () => {
    for (const [english, text] of Object.entries(georgian.ui)) {
      const tags = (s: string) => (s.match(/<[^>]+>/g) ?? []).sort();
      expect(tags(text)).withContext(english).toEqual(tags(english));
      expect(tags(text).every((t) => /^<\/?code( class="num text-text")?>$/.test(t))).withContext(english).toBeTrue();
    }
  });

  it('has a title and content for each content page', () => {
    for (const slug of ['rules', 'no-winning-strategy']) {
      expect(georgian.pages[slug]?.title).toBeTruthy();
      expect(georgian.pages[slug]?.content).toBeTruthy();
    }
  });
});
