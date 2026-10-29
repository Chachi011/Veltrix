import { DIALOG_DATA } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { AdminApi } from '../../core/api/admin.api';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { I18n, mark } from '../../core/i18n/i18n';
import { LocalDatePipe, TranslateLabelsPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { AdminUserDetailsDto, UserRole } from '../../core/models/api.models';
import { ROLE_LABELS, TRANSACTION_TYPE_LABELS } from '../../shared/labels';
import { ToastStore } from '../../core/ui/toast.store';
import { MultiplierPipe, VlxPipe } from '../../shared/pipes';
import { Avatar, Badge, BadgeTone, Button, DrawerShell, Field, Icons, ModalService, Skeleton, TabItem, Tabs } from '../../shared/ui';
import { twoDecimals } from '../../shared/validators';

export const ROLE_TONES: Record<UserRole, BadgeTone> = { Player: 'neutral', Operator: 'accent', Admin: 'primary' };

/** A new idempotency key for the next adjustment (one per submitted form). */
function newRequestId(): string {
  return typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

/**
 * Everything about one user, and the admin actions: change role, block / unblock, adjust the balance.
 * The server enforces every rule again (no self role change / block / adjustment, never below zero);
 * the drawer only explains them. Default change detection: it shows reactive-form errors.
 */
@Component({
  selector: 'vx-user-drawer',
  imports: [
    LocalDatePipe,
    TranslatePipe,
    TranslateLabelsPipe,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule,
    MultiplierPipe,
    VlxPipe,
    Avatar,
    Badge,
    Button,
    DrawerShell,
    Field,
    Skeleton,
    Tabs
  ],
  changeDetection: ChangeDetectionStrategy.Default,
  templateUrl: './user-drawer.html'
})
export class UserDrawer {
  private readonly userId = inject<number>(DIALOG_DATA);
  private readonly api = inject(AdminApi);
  private readonly auth = inject(AuthStore);
  private readonly modal = inject(ModalService);
  private readonly toasts = inject(ToastStore);
  private readonly i18n = inject(I18n);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icons = Icons;
  protected readonly roles: UserRole[] = ['Player', 'Operator', 'Admin'];
  protected readonly roleTones = ROLE_TONES;
  protected readonly roleLabels = ROLE_LABELS;
  protected readonly txLabels = TRANSACTION_TYPE_LABELS;
  protected readonly directions: TabItem[] = [
    { value: 'add', label: mark('Add VLX') },
    { value: 'remove', label: mark('Remove VLX') }
  ];

  protected readonly user = signal<AdminUserDetailsDto | null>(null);
  protected readonly isSelf = computed(() => this.user()?.id === this.auth.user()?.id);

  protected readonly role = new FormControl<UserRole>('Player', { nonNullable: true });
  protected readonly savingRole = signal(false);

  protected readonly blockReason = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] });
  protected readonly savingBlock = signal(false);

  protected readonly direction = signal('add');
  protected readonly adjustForm = this.fb.group({
    amount: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01), Validators.max(1_000_000), twoDecimals]),
    reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
  });
  protected readonly adjusting = signal(false);
  private requestId = newRequestId();

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    const user = await firstValueFrom(this.api.user(this.userId)).catch(() => null);
    if (user) {
      this.apply(user);
    }
  }

  private apply(user: AdminUserDetailsDto): void {
    this.user.set(user);
    this.role.reset(user.role);
  }

  protected async saveRole(): Promise<void> {
    const user = this.user();
    const role = this.role.value;
    if (!user || role === user.role) {
      return;
    }
    const roleName = this.i18n.t(ROLE_LABELS[role]);
    const confirmed = await this.modal.confirm({
      title: this.i18n.t('Change the role of {nickname} to {role}?', { nickname: user.nickname, role: roleName }),
      message:
        role === 'Player'
          ? this.i18n.t('They lose access to the staff panels.')
          : role === 'Admin'
            ? this.i18n.t('Admins can manage every user, the settings and the balances.')
            : this.i18n.t('Operators can edit games and cancel rounds. Staff accounts cannot bet.'),
      confirmText: this.i18n.t('Change role'),
      cancelText: this.i18n.t('Cancel')
    });
    if (!confirmed) {
      this.role.reset(user.role);
      return;
    }

    this.savingRole.set(true);
    try {
      this.apply(await firstValueFrom(this.api.changeRole(user.id, role)));
      this.toasts.success(this.i18n.t('{nickname} is now {role}.', { nickname: user.nickname, role: roleName }));
    } catch {
      this.role.reset(user.role);
    } finally {
      this.savingRole.set(false);
    }
  }

  protected async toggleBlock(): Promise<void> {
    const user = this.user();
    if (!user || this.blockReason.invalid) {
      return;
    }
    const block = !user.isBlocked;
    if (block) {
      const confirmed = await this.modal.confirm({
        title: this.i18n.t('Block {nickname}?', { nickname: user.nickname }),
        message: this.i18n.t('They are signed out on every device at once and cannot log in or bet until unblocked.'),
        confirmText: this.i18n.t('Block account'),
        cancelText: this.i18n.t('Cancel'),
        tone: 'danger'
      });
      if (!confirmed) {
        return;
      }
    }

    this.savingBlock.set(true);
    try {
      const reason = this.blockReason.value.trim() || null;
      this.apply(await firstValueFrom(this.api.setBlocked(user.id, block, reason)));
      this.blockReason.reset('');
      this.toasts.success(
        block
          ? this.i18n.t('{nickname} is blocked.', { nickname: user.nickname })
          : this.i18n.t('{nickname} is unblocked.', { nickname: user.nickname })
      );
    } catch {
      // Shown by the error interceptor
    } finally {
      this.savingBlock.set(false);
    }
  }

  protected async adjust(): Promise<void> {
    const user = this.user();
    if (!user) {
      return;
    }
    if (this.adjustForm.invalid) {
      this.adjustForm.markAllAsTouched();
      return;
    }
    const { amount, reason } = this.adjustForm.getRawValue();
    const signed = this.direction() === 'add' ? amount! : -amount!;

    const amountText = Math.abs(signed).toFixed(2);
    const confirmed = await this.modal.confirm({
      title: signed > 0 ? this.i18n.t('Add {amount} VLX?', { amount: amountText }) : this.i18n.t('Remove {amount} VLX?', { amount: amountText }),
      message: this.i18n.t('{nickname} will see this change and your reason ("{reason}") in their transaction history.', {
        nickname: user.nickname,
        reason: reason.trim()
      }),
      confirmText: signed > 0 ? this.i18n.t('Add VLX') : this.i18n.t('Remove VLX'),
      cancelText: this.i18n.t('Cancel'),
      tone: signed > 0 ? 'primary' : 'danger'
    });
    if (!confirmed) {
      return;
    }

    this.adjusting.set(true);
    try {
      // The same requestId on a retry or double-click cannot apply the change twice (server idempotency)
      const result = await firstValueFrom(
        this.api.adjustBalance(user.id, { amount: signed, reason: reason.trim(), requestId: this.requestId })
      );
      this.requestId = newRequestId();
      this.adjustForm.reset({ amount: null, reason: '' });
      this.toasts.success(
        this.i18n.t('Balance of {nickname}: {before} → {after} VLX.', {
          nickname: user.nickname,
          before: result.balanceBefore.toFixed(2),
          after: result.balanceAfter.toFixed(2)
        })
      );
      await this.load();
    } catch (error) {
      applyServerErrors(this.adjustForm, error);
    } finally {
      this.adjusting.set(false);
    }
  }
}
