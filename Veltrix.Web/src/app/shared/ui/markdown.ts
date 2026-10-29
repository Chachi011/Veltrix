import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface InlineSegment {
  text: string;
  strong: boolean;
  em: boolean;
}

export type MarkdownBlock =
  | { kind: 'p'; segments: InlineSegment[] }
  | { kind: 'ol' | 'ul'; items: InlineSegment[][] };

const ORDERED = /^\d+\.\s+/;
const BULLET = /^[-*]\s+/;

/** "**bold** and *italic*" → segments. Anything else stays plain text. */
export function parseInline(text: string): InlineSegment[] {
  const segments: InlineSegment[] = [];
  const pattern = /\*\*(.+?)\*\*|\*(.+?)\*/g;
  let last = 0;
  for (const match of text.matchAll(pattern)) {
    if (match.index! > last) {
      segments.push({ text: text.slice(last, match.index), strong: false, em: false });
    }
    segments.push(match[1] !== undefined
      ? { text: match[1], strong: true, em: false }
      : { text: match[2], strong: false, em: true });
    last = match.index! + match[0].length;
  }
  if (last < text.length) {
    segments.push({ text: text.slice(last), strong: false, em: false });
  }
  return segments;
}

/**
 * The small Markdown subset of the content pages: paragraphs, numbered and bulleted lists, **bold**, *italic*.
 * It produces DATA that the template draws, never HTML: text from the database can never inject markup or scripts.
 */
export function parseMarkdown(source: string): MarkdownBlock[] {
  return source
    .replace(/\r\n/g, '\n')
    .split(/\n\s*\n/)
    .map((block) => block.split('\n').map((line) => line.trim()).filter(Boolean))
    .filter((lines) => lines.length > 0)
    .map((lines): MarkdownBlock => {
      if (lines.every((l) => ORDERED.test(l))) {
        return { kind: 'ol', items: lines.map((l) => parseInline(l.replace(ORDERED, ''))) };
      }
      if (lines.every((l) => BULLET.test(l))) {
        return { kind: 'ul', items: lines.map((l) => parseInline(l.replace(BULLET, ''))) };
      }
      return { kind: 'p', segments: parseInline(lines.join(' ')) };
    });
}

/** Renders content-page Markdown with design-system typography: <vx-markdown [source]="page.content" /> */
@Component({
  selector: 'vx-markdown',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-4 text-sm leading-6' },
  template: `
    <!-- One line on purpose: line breaks here would become spaces in the text ("**1.00x** ." instead of "1.00x.") -->
    <ng-template #inline let-segments>@for (s of segments; track $index) {@if (s.strong) {<strong class="font-semibold text-text">{{ s.text }}</strong>} @else if (s.em) {<em>{{ s.text }}</em>} @else {<ng-container>{{ s.text }}</ng-container>}}</ng-template>

    @for (block of blocks(); track $index) {
      @switch (block.kind) {
        @case ('p') {
          <p class="text-muted"><ng-container *ngTemplateOutlet="inline; context: { $implicit: $any(block).segments }" /></p>
        }
        @case ('ol') {
          <ol class="flex list-decimal flex-col gap-2 pl-5 text-muted marker:font-mono marker:text-primary-text">
            @for (item of $any(block).items; track $index) {
              <li class="pl-1"><ng-container *ngTemplateOutlet="inline; context: { $implicit: item }" /></li>
            }
          </ol>
        }
        @case ('ul') {
          <ul class="flex list-disc flex-col gap-2 pl-5 text-muted marker:text-primary-text">
            @for (item of $any(block).items; track $index) {
              <li class="pl-1"><ng-container *ngTemplateOutlet="inline; context: { $implicit: item }" /></li>
            }
          </ul>
        }
      }
    }
  `
})
export class Markdown {
  readonly source = input.required<string>();
  protected readonly blocks = computed(() => parseMarkdown(this.source()));
}
