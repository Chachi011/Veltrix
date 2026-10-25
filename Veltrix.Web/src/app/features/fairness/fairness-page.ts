import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FairnessApi } from '../../core/api/fairness.api';
import { FairnessRoundDto, VerifyFairnessResult } from '../../core/models/api.models';
import { LucideAngularModule } from 'lucide-angular';
import { MultiplierPipe } from '../../shared/pipes';
import { Badge, Button, Card, CopyButton, EmptyState, Field, Icons, PageHeader } from '../../shared/ui';
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
  imports: [NgTemplateOutlet, ReactiveFormsModule, LucideAngularModule, MultiplierPipe, Badge, Button, Card, CopyButton, EmptyState, Field, PageHeader],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './fairness-page.html'
})
export class FairnessPage {
  private readonly api = inject(FairnessApi);
  protected readonly icons = Icons;

  /** Shown as numbered steps (static, trusted text, so innerHTML is safe here). */
  protected readonly steps = [
    'Before betting opens, the server publishes <code class="num text-text">SHA-256(seed)</code> of a secret random seed.',
    'The crash point comes only from that seed: <code class="num text-text">h</code> = first 52 bits of <code class="num text-text">HMAC-SHA256(seed, "VELTRIX")</code>, <code class="num text-text">r = h / 2^52</code>.',
    '<code class="num text-text">crash = max(1.00, floor(100 · 0.97 / (1 − r)) / 100)</code>',
    'After the crash the seed is revealed. If it matches the hash and gives the same crash point, the result was fixed in advance.',
    'The 3% house edge means every strategy returns 97% on average: there is no winning system.'
  ];

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
