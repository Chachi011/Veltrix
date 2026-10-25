import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ProfileApi } from '../../core/api/profile.api';
import { AuthStore } from '../../core/auth/auth.store';
import { applyServerErrors } from '../../core/http/problem';
import { ProfileDto } from '../../core/models/api.models';
import { WalletStore } from '../../core/state/wallet.store';
import { ToastStore } from '../../core/ui/toast.store';
import { FieldError } from '../../shared/field-error';
import { VlxPipe } from '../../shared/pipes';
import { NICKNAME_PATTERN, fieldsMatch, strongPassword } from '../../shared/validators';

const MAX_AVATAR_BYTES = 2 * 1024 * 1024;
const AVATAR_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'vx-profile-page',
  imports: [DatePipe, ReactiveFormsModule, FieldError, VlxPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss'
})
export class ProfilePage {
  private readonly api = inject(ProfileApi);
  private readonly auth = inject(AuthStore);
  private readonly wallet = inject(WalletStore);
  private readonly toasts = inject(ToastStore);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly profile = signal<ProfileDto | null>(null);
  protected readonly preview = signal<string | null>(null);
  protected readonly avatarError = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly savingNickname = signal(false);
  protected readonly savingPassword = signal(false);

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
    if (profile) {
      this.applyProfile(profile);
    }
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.clearPreview();
    this.avatarError.set(null);
    this.selectedFile = null;

    if (!file) {
      return;
    }
    // Quick checks in the browser; the server checks again (including the real file content)
    if (!AVATAR_TYPES.includes(file.type)) {
      this.avatarError.set('Only JPG, PNG or WEBP images.');
      input.value = '';
      return;
    }
    if (file.size > MAX_AVATAR_BYTES) {
      this.avatarError.set('The image must be 2 MB or smaller.');
      input.value = '';
      return;
    }

    this.selectedFile = file;
    this.preview.set(URL.createObjectURL(file));
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
      this.toasts.success('Avatar updated.');
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
      this.toasts.success('Nickname saved.');
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
      this.toasts.success('Password changed. Please log in with your new password.');
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
