import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FairnessApi } from '../../core/api/fairness.api';
import { FairnessRoundDto, VerifyFairnessResult } from '../../core/models/api.models';
import { FieldError } from '../../shared/field-error';
import { MultiplierPipe } from '../../shared/pipes';
import { crashPointFromSeed, sha256Hex } from '../crash/crash-math';

interface BrowserCheck {
  hash: string;
  crashPoint: number;
  hashMatches: boolean | null;
  crashPointMatches: boolean | null;
}

/**
 * Provably fair verification. The result is checked twice:
 *  1. by the server (POST /api/fairness/verify)
 *  2. by THIS browser with the Web Crypto API, so the player does not have to trust the server at all.
 */
@Component({
  selector: 'vx-fairness-page',
  imports: [ReactiveFormsModule, FieldError, MultiplierPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './fairness-page.html',
  styleUrl: './fairness-page.scss'
})
export class FairnessPage {
  private readonly api = inject(FairnessApi);

  /** ?round=123 (bound from the query string). */
  readonly round = input<string>();

  protected readonly roundInfo = signal<FairnessRoundDto | null>(null);
  protected readonly serverResult = signal<VerifyFairnessResult | null>(null);
  protected readonly browserResult = signal<BrowserCheck | null>(null);
  protected readonly verifying = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    serverSeed: ['', [Validators.required, Validators.maxLength(200)]],
    serverSeedHash: ['', Validators.pattern(/^[0-9a-fA-F]{64}$/)],
    crashPoint: ['']
  });

  constructor() {
    effect(() => {
      const id = Number(this.round());
      untracked(() => {
        if (Number.isInteger(id) && id > 0) {
          void this.loadRound(id);
        }
      });
    });
  }

  private async loadRound(roundId: number): Promise<void> {
    this.serverResult.set(null);
    this.browserResult.set(null);

    const info = await firstValueFrom(this.api.round(roundId)).catch(() => null);
    this.roundInfo.set(info);

    if (info?.serverSeed) {
      this.form.setValue({
        serverSeed: info.serverSeed,
        serverSeedHash: info.serverSeedHash,
        crashPoint: info.crashPoint?.toFixed(2) ?? ''
      });
      await this.verify();
    }
  }

  protected async verify(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { serverSeed, serverSeedHash, crashPoint } = this.form.getRawValue();
    const seed = serverSeed.trim();
    const hash = serverSeedHash.trim().toLowerCase() || null;
    const expected = crashPoint ? Number(crashPoint) : null;

    this.verifying.set(true);
    try {
      // Both checks run at the same time; the browser one never touches the network
      const [server, browserHash, browserCrash] = await Promise.all([
        firstValueFrom(this.api.verify({ serverSeed: seed, serverSeedHash: hash, crashPoint: expected })).catch(() => null),
        sha256Hex(seed),
        crashPointFromSeed(seed)
      ]);

      this.serverResult.set(server);
      this.browserResult.set({
        hash: browserHash,
        crashPoint: browserCrash,
        hashMatches: hash ? browserHash === hash : null,
        crashPointMatches: expected !== null ? browserCrash === expected : null
      });
    } finally {
      this.verifying.set(false);
    }
  }

  protected verdict(hashMatches: boolean | null, crashMatches: boolean | null): 'ok' | 'fail' | 'partial' {
    if (hashMatches === false || crashMatches === false) return 'fail';
    if (hashMatches && crashMatches) return 'ok';
    return 'partial';
  }
}
