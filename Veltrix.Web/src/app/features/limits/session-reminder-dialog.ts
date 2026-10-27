import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
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
  imports: [LucideAngularModule, Button, ModalShell, VlxPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <vx-modal title="Time for a break?" description="A reminder you set on the Responsible play page.">
      <div class="flex items-start gap-4">
        <span class="grid size-12 shrink-0 place-items-center rounded-full border border-warning/25 bg-warning/12 text-warning">
          <lucide-icon [img]="icons.hourglass" [size]="22" aria-hidden="true" />
        </span>
        <div class="min-w-0 text-sm">
          <p>
            You have been playing for <strong class="num">{{ data.minutes }}</strong> minutes:
            <span class="num">{{ data.bets }}</span> bets, <span class="num">{{ data.wagered | vlx }}</span> staked.
          </p>
          <p class="mt-2 text-[13px] text-muted">Crash has no winning strategy: every round is independent and the house keeps 3% on average.</p>
        </div>
      </div>
      <ng-container modal-footer>
        <button vxButton type="button" variant="ghost" (click)="ref.close('limits')">Review my limits</button>
        <button vxButton type="button" variant="primary" (click)="ref.close('continue')">Keep playing</button>
      </ng-container>
    </vx-modal>
  `
})
export class SessionReminderDialog {
  protected readonly data = inject<ReminderData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<ReminderChoice>>(DialogRef);
  protected readonly icons = Icons;
}
