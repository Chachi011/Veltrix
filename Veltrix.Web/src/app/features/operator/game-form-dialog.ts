import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { applyServerErrors } from '../../core/http/problem';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { GameDto, SaveGameRequest } from '../../core/models/api.models';
import { Button, Field, ModalShell } from '../../shared/ui';
import { twoDecimals } from '../../shared/validators';

/** Same rule as the server's validator: lowercase words joined by single dashes. */
export const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

/** Group validator: the maximum bet may not be below the minimum. */
const maxNotBelowMin = (group: AbstractControl): ValidationErrors | null => {
  const min = Number(group.get('minBet')?.value);
  const max = group.get('maxBet');
  if (!max || !Number.isFinite(min) || max.value === null || max.value === '') {
    return null;
  }
  const { maxBelowMin: _, ...others } = max.errors ?? {};
  max.setErrors(Number(max.value) < min ? { ...others, maxBelowMin: true } : Object.keys(others).length ? others : null);
  return null;
};

/** Create or edit a game. Default change detection: it shows reactive-form errors. */
@Component({
  selector: 'vx-game-form-dialog',
  imports: [ReactiveFormsModule, Button, Field, ModalShell, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <vx-modal
      [title]="game ? ('Edit {name}' | t: { name: game.name }) : ('New game' | t)"
      [description]="'Bet range and round timing apply from the next round.' | t"
    >
      <form id="game-form" class="flex flex-col gap-4" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <div class="grid gap-4 sm:grid-cols-2">
          <vx-field [label]="'Name' | t" for="name" [control]="form.controls.name">
            <input id="name" class="vx-input" formControlName="name" maxlength="100" />
          </vx-field>
          <vx-field
            [label]="'Slug' | t"
            for="slug"
            [control]="form.controls.slug"
            [hint]="'Used in links, e.g. crash-classic.' | t"
            [patternMessage]="'Lowercase letters, digits and single dashes.' | t"
          >
            <input id="slug" class="vx-input num" formControlName="slug" maxlength="50" />
          </vx-field>
        </div>
        <vx-field [label]="'Description' | t" for="description" [control]="form.controls.description">
          <textarea id="description" class="vx-input h-20! py-2" formControlName="description" maxlength="2000"></textarea>
        </vx-field>
        <div class="grid grid-cols-2 gap-4">
          <vx-field [label]="'Min bet (VLX)' | t" for="minBet" [control]="form.controls.minBet">
            <input id="minBet" class="vx-input num" type="number" inputmode="decimal" formControlName="minBet" />
          </vx-field>
          <vx-field [label]="'Max bet (VLX)' | t" for="maxBet" [control]="form.controls.maxBet">
            <input id="maxBet" class="vx-input num" type="number" inputmode="decimal" formControlName="maxBet" />
          </vx-field>
          <vx-field [label]="'Betting time (s)' | t" for="betting" [control]="form.controls.bettingDurationSec" [hint]="'3 to 120 seconds.' | t">
            <input id="betting" class="vx-input num" type="number" formControlName="bettingDurationSec" />
          </vx-field>
          <vx-field
            [label]="'Pause between rounds (s)' | t"
            for="pause"
            [control]="form.controls.pauseBetweenRoundsSec"
            [hint]="'0 to 120 seconds.' | t"
          >
            <input id="pause" class="vx-input num" type="number" formControlName="pauseBetweenRoundsSec" />
          </vx-field>
        </div>
      </form>
      <ng-container modal-footer>
        <button vxButton type="button" variant="ghost" (click)="ref.close()">{{ 'Cancel' | t }}</button>
        <button vxButton type="submit" form="game-form" variant="primary" [loading]="saving()">
          {{ game ? ('Save changes' | t) : ('Create game' | t) }}
        </button>
      </ng-container>
    </vx-modal>
  `
})
export class GameFormDialog {
  protected readonly game = inject<GameDto | null>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<GameDto>>(DialogRef);
  private readonly api = inject(GamesApi);
  protected readonly saving = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      name: [this.game?.name ?? '', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      slug: [this.game?.slug ?? '', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern(SLUG_PATTERN)]],
      description: [this.game?.description ?? '', [Validators.maxLength(2000)]],
      minBet: [this.game?.minBet ?? 1, [Validators.required, Validators.min(0.01), twoDecimals]],
      maxBet: [this.game?.maxBet ?? 1000, [Validators.required, Validators.max(1_000_000), twoDecimals]],
      bettingDurationSec: [this.game?.bettingDurationSec ?? 10, [Validators.required, Validators.min(3), Validators.max(120)]],
      pauseBetweenRoundsSec: [this.game?.pauseBetweenRoundsSec ?? 5, [Validators.required, Validators.min(0), Validators.max(120)]]
    },
    { validators: maxNotBelowMin }
  );

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const request: SaveGameRequest = { ...value, description: value.description.trim() || null };

    this.saving.set(true);
    try {
      const saved = await firstValueFrom(this.game ? this.api.update(this.game.id, request) : this.api.create(request));
      this.ref.close(saved);
    } catch (error) {
      applyServerErrors(this.form, error);
    } finally {
      this.saving.set(false);
    }
  }
}
