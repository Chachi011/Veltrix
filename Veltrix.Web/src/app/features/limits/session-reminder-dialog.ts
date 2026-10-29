import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { TranslatePipe } from '../../core/i18n/i18n-pipes';
import { VlxPipe } from '../../shared/pipes';
import { Button, Icons, ModalShell } from '../../shared/ui';

export interface ReminderData {
  minutes: number;
  bets: number;
  wagered: number;
}

export type ReminderChoice = 'limits' | 'continue';

/**
 * "You have been playing for 60 minutes": a reality check, with a way to the limits page.
 * Its own file, loaded only when a reminder is due (it is not part of the initial bundle).
 */
@Component({
  selector: 'vx-session-reminder-dialog',
  imports: [LucideAngularModule, Button, ModalShell, VlxPipe, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-modal [title]="'Time for a break?' | t" [description]="'A reminder you set on the Responsible play page.' | t">
      <div class="flex items-start gap-4">
        <span class="grid size-12 shrink-0 place-items-center rounded-full border border-warning/25 bg-warning/12 text-warning">
          <lucide-icon [img]="icons.hourglass" [size]="22" aria-hidden="true" />
        </span>
        <div class="min-w-0 text-sm">
          <p>
            {{
              'You have been playing for {minutes} minutes: {bets} bets, {amount} staked.'
                | t: { minutes: data.minutes, bets: data.bets, amount: (data.wagered | vlx) }
            }}
          </p>
          <p class="mt-2 text-[13px] text-muted">
            {{ 'Crash has no winning strategy: every round is independent and the house keeps 3% on average.' | t }}
          </p>
        </div>
      </div>
      <ng-container modal-footer>
        <button vxButton type="button" variant="ghost" (click)="ref.close('limits')">{{ 'Review my limits' | t }}</button>
        <button vxButton type="button" variant="primary" (click)="ref.close('continue')">{{ 'Keep playing' | t }}</button>
      </ng-container>
    </vx-modal>
  `
})
export class SessionReminderDialog {
  protected readonly data = inject<ReminderData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<ReminderChoice>>(DialogRef);
  protected readonly icons = Icons;
}
