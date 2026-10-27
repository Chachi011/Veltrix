import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { ThemePreference } from '../../core/models/api.models';
import { PreferencesStore } from '../../core/state/preferences.store';
import { SoundService } from '../../core/ui/sound.service';
import { Button, Card, Icons, PageHeader, TabItem, Tabs } from '../../shared/ui';

const THEMES: TabItem[] = [
  { value: 'System', label: 'System' },
  { value: 'Dark', label: 'Dark' },
  { value: 'Light', label: 'Light' }
];

/**
 * Settings (spec 4.7): dark / light theme, game sounds, animations.
 * Each change applies at once and is saved on the account, so it follows the player to any device.
 */
@Component({
  selector: 'vx-settings-page',
  imports: [LucideAngularModule, Button, Card, PageHeader, Tabs],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-page-header title="Settings" description="Saved on your account, so they follow you to any device." />

    <div class="grid gap-4 md:gap-6 lg:grid-cols-2">
      <vx-card title="Appearance" subtitle="System follows your device's dark or light setting.">
        <div class="flex flex-col gap-5">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <span class="text-sm font-medium">Theme</span>
            <vx-tabs [items]="themes" [value]="prefs.theme()" (valueChange)="setTheme($event)" label="Theme" />
          </div>

          <label class="flex items-start justify-between gap-4">
            <span>
              <span class="block text-sm font-medium">Animations</span>
              <span class="block text-[13px] text-muted">Counting numbers, chart and shake effects. Off = reduce motion.</span>
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

      <vx-card title="Sound" subtitle="Short sounds for your own bet, cash-out and a lost round.">
        <div class="flex flex-col gap-5">
          <label class="flex items-start justify-between gap-4">
            <span>
              <span class="block text-sm font-medium">Game sounds</span>
              <span class="block text-[13px] text-muted">Played only for your own actions, never for other players.</span>
            </span>
            <input
              type="checkbox"
              class="vx-switch mt-1"
              [checked]="prefs.soundEnabled()"
              (change)="prefs.update({ soundEnabled: $any($event.target).checked })"
            />
          </label>
          <div class="flex flex-wrap gap-2">
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('bet')">Bet</button>
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('cashout')">Cash-out</button>
            <button vxButton type="button" variant="secondary" size="sm" [disabled]="!prefs.soundEnabled()" (click)="sound.play('crash')">Lost round</button>
          </div>
        </div>
      </vx-card>
    </div>

    <p class="mt-6 flex items-start gap-2 text-xs text-muted" role="status" aria-live="polite">
      <lucide-icon [img]="icons.info" [size]="14" class="mt-px shrink-0" aria-hidden="true" />
      {{ prefs.saving() ? 'Saving…' : 'Changes are saved automatically. Your device\\'s "reduce motion" setting is always respected too.' }}
    </p>
  `
})
export class SettingsPage {
  protected readonly prefs = inject(PreferencesStore);
  protected readonly sound = inject(SoundService);
  protected readonly icons = Icons;
  protected readonly themes = THEMES;

  protected setTheme(value: string): void {
    void this.prefs.update({ theme: value as ThemePreference });
  }
}
