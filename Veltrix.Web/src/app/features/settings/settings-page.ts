import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { mark } from '../../core/i18n/i18n';
import { TranslateLabelsPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { LanguageCode, ThemePreference } from '../../core/models/api.models';
import { PreferencesStore } from '../../core/state/preferences.store';
import { SoundService } from '../../core/ui/sound.service';
import { Button, Card, Icons, PageHeader, TabItem, Tabs } from '../../shared/ui';

const THEMES: TabItem[] = [
  { value: 'System', label: mark('System') },
  { value: 'Dark', label: mark('Dark') },
  { value: 'Light', label: mark('Light') }
];

/** Each language is named in its own language, so it can be found by someone who cannot read the other. */
const LANGUAGES: TabItem[] = [
  { value: 'ka', label: 'ქართული' },
  { value: 'en', label: 'English' }
];

/**
 * Settings (spec 4.7): language, dark / light theme, game sounds, animations.
 * Each change applies at once and is saved on the account, so it follows the player to any device.
 */
@Component({
  selector: 'vx-settings-page',
  imports: [LucideAngularModule, Button, Card, PageHeader, Tabs, TranslatePipe, TranslateLabelsPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header [title]="'Settings' | t" [description]="'Saved on your account, so they follow you to any device.' | t" />

    <div class="grid gap-4 md:gap-6 lg:grid-cols-2">
      <vx-card [title]="'Language' | t" [subtitle]="'Georgian is the default. Crash, Cash out and VLX keep their names.' | t">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <span class="text-sm font-medium">{{ 'Interface language' | t }}</span>
          <vx-tabs [items]="languages" [value]="prefs.language()" (valueChange)="setLanguage($event)" [label]="'Language' | t" />
        </div>
      </vx-card>

      <vx-card [title]="'Appearance' | t" [subtitle]="'System follows the dark or light setting of your device.' | t">
        <div class="flex flex-col gap-5">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <span class="text-sm font-medium">{{ 'Theme' | t }}</span>
            <vx-tabs [items]="themes | tLabels" [value]="prefs.theme()" (valueChange)="setTheme($event)" [label]="'Theme' | t" />
          </div>

          <label class="flex items-start justify-between gap-4">
            <span>
              <span class="block text-sm font-medium">{{ 'Animations' | t }}</span>
              <span class="block text-[13px] text-muted">{{ 'Counting numbers, chart and shake effects. Off = reduce motion.' | t }}</span>
            </span>
            <input
              type="checkbox"
              class="vx-switch mt-1"
              [checked]="prefs.animationsEnabled()"
              (change)="prefs.update({ animationsEnabled: $any($event.target).checked })"
            />
          </label>
        </div>
      </vx-card>

      <vx-card [title]="'Sound' | t" [subtitle]="'Short sounds for your own bet, cash-out and a lost round.' | t">
        <div class="flex flex-col gap-5">
          <label class="flex items-start justify-between gap-4">
            <span>
              <span class="block text-sm font-medium">{{ 'Game sounds' | t }}</span>
              <span class="block text-[13px] text-muted">{{ 'Played only for your own actions, never for other players.' | t }}</span>
            </span>
            <input
              type="checkbox"
              class="vx-switch mt-1"
              [checked]="prefs.soundEnabled()"
              (change)="prefs.update({ soundEnabled: $any($event.target).checked })"
            />
          </label>
          <div class="flex flex-wrap gap-2">
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('bet')">
              {{ 'Bet' | t }}
            </button>
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('cashout')">
              Cash out
            </button>
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('crash')">
              {{ 'Lost round' | t }}
            </button>
          </div>
        </div>
      </vx-card>
    </div>

    <p class="mt-6 flex items-start gap-2 text-xs text-muted" role="status" aria-live="polite">
      <lucide-icon [img]="icons.info" [size]="14" class="mt-px shrink-0" aria-hidden="true" />
      {{
        prefs.saving()
          ? ('Saving…' | t)
          : ('Changes are saved automatically. The reduce-motion setting of your device is always respected too.' | t)
      }}
    </p>
  `
})
export class SettingsPage {
  protected readonly prefs = inject(PreferencesStore);
  protected readonly sound = inject(SoundService);
  protected readonly icons = Icons;
  protected readonly themes = THEMES;
  protected readonly languages = LANGUAGES;

  protected setTheme(value: string): void {
    void this.prefs.update({ theme: value as ThemePreference });
  }

  protected setLanguage(value: string): void {
    void this.prefs.update({ language: value as LanguageCode });
  }
}
