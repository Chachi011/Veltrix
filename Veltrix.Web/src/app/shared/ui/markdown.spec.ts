import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Markdown, parseInline, parseMarkdown } from './markdown';

describe('Markdown subset (content pages)', () => {
  it('turns a numbered block into an ordered list and the rest into paragraphs', () => {
    const blocks = parseMarkdown('1. Bet\n2. **Cash out**\n\nVLX points are *virtual*.');

    expect(blocks.length).toBe(2);
    expect(blocks[0].kind).toBe('ol');
    expect(blocks[1].kind).toBe('p');
  });

  it('reads **bold** and *italic*', () => {
    expect(parseInline('get *bet × multiplier* or **lose** it')).toEqual([
      { text: 'get ', strong: false, em: false },
      { text: 'bet × multiplier', strong: false, em: true },
      { text: ' or ', strong: false, em: false },
      { text: 'lose', strong: true, em: false },
      { text: ' it', strong: false, em: false }
    ]);
  });

  it('joins the lines of a paragraph and understands bullet lists', () => {
    const blocks = parseMarkdown('A long series of low crashes\ndoes not change the odds.\n\n- martingale\n- always 2x');

    expect(blocks[0]).toEqual({ kind: 'p', segments: [{ text: 'A long series of low crashes does not change the odds.', strong: false, em: false }] });
    expect(blocks[1].kind).toBe('ul');
  });

  it('keeps HTML as plain text (the page never renders markup from the database)', () => {
    const [block] = parseMarkdown('<img src=x onerror=alert(1)>');
    expect(block).toEqual({ kind: 'p', segments: [{ text: '<img src=x onerror=alert(1)>', strong: false, em: false }] });
  });

  // Bug found in the browser: template line breaks became spaces ("grows from 1.00x ." on the Rules page)
  it('renders bold and italic without extra spaces around them', async () => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    const fixture = TestBed.createComponent(Markdown);
    fixture.componentRef.setInput('source', 'The multiplier grows from **1.00x**. You get *bet × multiplier*.');
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent!.replace(/\s+/g, ' ').trim();
    expect(text).toBe('The multiplier grows from 1.00x. You get bet × multiplier.');
    expect((fixture.nativeElement as HTMLElement).querySelector('img')).toBeNull();
  });
});
