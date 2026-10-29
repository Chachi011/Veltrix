import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games.api';
import { TranslatePipe, TranslateSplitPipe } from '../../core/i18n/i18n-pipes';
import { RoundCancellationDto } from '../../core/models/api.models';
import { VlxPipe } from '../../shared/pipes';
import { Button, Field, Icons, ModalShell } from '../../shared/ui';

export interface CancelRoundData {
  roundId: number;
  roundNumber: number;
  activeBets: number;
  activeTotal: number;
}

/** Cancel the live round with a reason (stored in the audit log). The server refunds every active bet. */
@Component({
  selector: 'vx-cancel-round-dialog',
  imports: [ReactiveFormsModule, LucideAngularModule, VlxPipe, Button, Field, ModalShell, TranslatePipe, TranslateSplitPipe],
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <vx-modal
      [title]="'Cancel round #{number}?' | t: { number: data.roundNumber }"
      [description]="'Use this only if something went wrong (e.g. a technical problem).' | t"
    >
      <div class="mb-4 flex items-start gap-3 rounded-control border border-warning/25 bg-warning/12 p-3 text-[13px]">
        <lucide-icon [img]="icons.alert" [size]="16" class="mt-0.5 shrink-0 text-warning" aria-hidden="true" />
        @let refund = '{count} active bets ({value}) will be refunded.' | tSplit: { count: data.activeBets };
        <span>
          {{ refund.before }}<span class="num font-semibold">{{ data.activeTotal | vlx }}</span>{{ refund.after }}
          {{ 'Bets already cashed out keep their payout.' | t }}
        </span>
      </div>
      <form id="cancel-form" (ngSubmit)="submit()" novalidate>
        <vx-field [label]="'Reason' | t" for="reason" [control]="reason" [hint]="'5 to 500 characters. Saved in the audit log.' | t">
          <textarea id="reason" class="vx-input h-24! py-2" [formControl]="reason" maxlength="500"></textarea>
        </vx-field>
      </form>
      <ng-container modal-footer>
        <button vxButton type="button" variant="ghost" (click)="ref.close()">{{ 'Keep the round' | t }}</button>
        <button vxButton type="submit" form="cancel-form" variant="danger" [loading]="saving()">
          <lucide-icon [img]="icons.stop" [size]="16" aria-hidden="true" />{{ 'Cancel round' | t }}
        </button>
      </ng-container>
    </vx-modal>
  `
})
export class CancelRoundDialog {
  protected readonly data = inject<CancelRoundData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<RoundCancellationDto>>(DialogRef);
  private readonly api = inject(GamesApi);
  protected readonly icons = Icons;
  protected readonly saving = signal(false);

  protected readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(5), Validators.maxLength(500)]
  });

  protected async submit(): Promise<void> {
    if (this.reason.invalid || this.reason.value.trim().length < 5) {
      this.reason.setErrors({ ...(this.reason.errors ?? {}), minlength: { requiredLength: 5 } });
      this.reason.markAsTouched();
      return;
    }
    this.saving.set(true);
    try {
      // The round may have crashed meanwhile: the server then answers 409 and the interceptor shows why
      this.ref.close(await firstValueFrom(this.api.cancelRound(this.data.roundId, this.reason.value.trim())));
    } catch {
      this.saving.set(false);
    }
  }
}
