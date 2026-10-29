import { Injectable, effect, inject, untracked } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { I18n } from './i18n';

const APP_NAME = 'VELTRIX';

/**
 * Browser tab titles in the current language. Routes keep English titles ("Wallet · VELTRIX");
 * the page part is translated, and translated again when the language changes.
 */
@Injectable({ providedIn: 'root' })
export class TranslatedTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly i18n = inject(I18n);
  private pageTitle = '';

  constructor() {
    super();
    effect(() => {
      this.i18n.language();
      untracked(() => this.apply());
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.pageTitle = (this.buildTitle(snapshot) ?? APP_NAME).split(' · ')[0];
    this.apply();
  }

  private apply(): void {
    if (!this.pageTitle || this.pageTitle === APP_NAME) {
      this.title.setTitle(APP_NAME);
      return;
    }
    this.title.setTitle(`${this.i18n.t(this.pageTitle)} · ${APP_NAME}`);
  }
}
