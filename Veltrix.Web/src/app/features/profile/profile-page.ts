import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/profile.api';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { I18n, mark } from '../../core/i18n/i18n';
import { LocalDatePipe, ServerTextPipe, TranslatePipe } from '../../core/i18n/i18n-pipes';
import { ProfileDto, PublicProfileDto } from '../../core/models/api.models';
import { WalletStore } from '../../core/state/wallet.store';
import { ToastStore } from '../../core/ui/toast.store';
import {
  AnimatedNumber,
  Avatar,
  Badge,
  Button,
  Card,
  Field,
  Icons,
  PageHeader,
  PasswordInput,
  Skeleton,
  StatCard
} from '../../shared/ui';
import { ROLE_LABELS } from '../../shared/labels';
import { NICKNAME_PATTERN, fieldsMatch, strongPassword } from '../../shared/validators';
import { MyStats } from './my-stats';

const MAX_AVATAR_BYTES = 2 * 1024 * 1024;
const AVATAR_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'vx-profile-page',
  imports: [
    LocalDatePipe,
    ServerTextPipe,
    TranslatePipe,
    ReactiveFormsModule,
    LucideAngularModule,
    AnimatedNumber,
    Avatar,
    Badge,
    Button,
    Card,
    Field,
    PageHeader,
    PasswordInput,
    Skeleton,
    StatCard,
    MyStats
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-page.html'
})
export class ProfilePage {
  private readonly api = inject(ProfileApi);
  private readonly auth = inject(AuthStore);
  private readonly wallet = inject(WalletStore);
  private readonly toasts = inject(ToastStore);
  private readonly i18n = inject(I18n);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icons = Icons;
  protected readonly roleLabels = ROLE_LABELS;
  protected readonly profile = signal<ProfileDto | null>(null);
  protected readonly stats = signal<PublicProfileDto | null>(null);
  protected readonly preview = signal<string | null>(null);
  protected readonly avatarError = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly savingNickname = signal(false);
  protected readonly savingPassword = signal(false);

  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private selectedFile: File | null = null;

  protected readonly nicknameForm = this.fb.group({
    nickname: ['', [Validators.required, Validators.pattern(NICKNAME_PATTERN)]]
  });

  protected readonly passwordForm = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, strongPassword]],
      confirmPassword: ['', Validators.required]
    },
    { validators: fieldsMatch('newPassword', 'confirmPassword') }
  );

  constructor() {
    void this.load();
    // The preview is a temporary browser URL; release it when leaving the page
    inject(DestroyRef).onDestroy(() => this.clearPreview());
  }

  private async load(): Promise<void> {
    const profile = await firstValueFrom(this.api.get()).catch(() => null);
    if (!profile) {
      return;
    }
    this.applyProfile(profile);
    // Level and best results come from the public profile (the same data other players see)
    this.stats.set(await firstValueFrom(this.api.getPublic(profile.nickname)).catch(() => null));
  }

  protected chooseFile(): void {
    this.fileInput()?.nativeElement.click();
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = ''; // choosing the same file again still fires (change)
    this.clearPreview();
    this.avatarError.set(null);
    this.selectedFile = null;

    if (!file) {
      return;
    }
    // Quick checks in the browser; the server checks again (including the real file content)
    if (!AVATAR_TYPES.includes(file.type)) {
      this.avatarError.set(mark('Only JPG, PNG or WEBP images.'));
      return;
    }
    if (file.size > MAX_AVATAR_BYTES) {
      this.avatarError.set(mark('The image must be 2 MB or smaller.'));
      return;
    }

    this.selectedFile = file;
    this.preview.set(URL.createObjectURL(file));
  }

  protected cancelAvatar(): void {
    this.clearPreview();
    this.selectedFile = null;
  }

  protected async uploadAvatar(): Promise<void> {
    if (!this.selectedFile) {
      return;
    }
    this.uploading.set(true);
    try {
      const profile = await firstValueFrom(this.api.uploadAvatar(this.selectedFile));
      this.applyProfile(profile);
      this.clearPreview();
      this.selectedFile = null;
      this.toasts.success(this.i18n.t('Avatar updated.'));
    } catch {
      // Shown by the error interceptor
    } finally {
      this.uploading.set(false);
    }
  }

  protected async saveNickname(): Promise<void> {
    if (this.nicknameForm.invalid) {
      this.nicknameForm.markAllAsTouched();
      return;
    }
    this.savingNickname.set(true);
    try {
      const profile = await firstValueFrom(this.api.update(this.nicknameForm.getRawValue().nickname));
      this.applyProfile(profile);
      this.toasts.success(this.i18n.t('Nickname saved.'));
    } catch (error) {
      applyServerErrors(this.nicknameForm, error);
    } finally {
      this.savingNickname.set(false);
    }
  }

  protected async changePassword(): Promise<void> {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.savingPassword.set(true);
    try {
      await firstValueFrom(this.api.changePassword({ currentPassword, newPassword }));
      this.passwordForm.reset();
      // The server revoked all refresh tokens: this session ends too, so log in again
      this.toasts.success(this.i18n.t('Password changed. Please log in with your new password.'));
      await this.auth.logout();
    } catch (error) {
      applyServerErrors(this.passwordForm, error);
    } finally {
      this.savingPassword.set(false);
    }
  }

  private applyProfile(profile: ProfileDto): void {
    this.profile.set(profile);
    this.nicknameForm.reset({ nickname: profile.nickname });
    this.auth.updateUser({ nickname: profile.nickname, avatarUrl: profile.avatarUrl });
    this.wallet.setBalance(profile.balance);
  }

  private clearPreview(): void {
    const url = this.preview();
    if (url) {
      URL.revokeObjectURL(url);
      this.preview.set(null);
    }
  }
}
