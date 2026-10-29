import { Injectable, inject } from '@angular/core';
import { PreferencesStore } from '../state/preferences.store';

export type GameSound = 'bet' | 'cashout' | 'crash';

interface Note {
  /** Seconds after the start of the sound. */
  at: number;
  frequency: number;
  duration: number;
  type: OscillatorType;
  volume: number;
}

/** Short synthesized sounds: no audio files to download, and nothing to license. */
const SOUNDS: Record<GameSound, Note[]> = {
  // A soft double click: "your bet is in"
  bet: [
    { at: 0, frequency: 660, duration: 0.06, type: 'triangle', volume: 0.12 },
    { at: 0.07, frequency: 880, duration: 0.08, type: 'triangle', volume: 0.1 }
  ],
  // A rising three-note chime: a win
  cashout: [
    { at: 0, frequency: 523.25, duration: 0.1, type: 'sine', volume: 0.16 },
    { at: 0.08, frequency: 659.25, duration: 0.1, type: 'sine', volume: 0.16 },
    { at: 0.16, frequency: 783.99, duration: 0.22, type: 'sine', volume: 0.16 }
  ],
  // A low falling thud: the round crashed with my bet still in it
  crash: [{ at: 0, frequency: 180, duration: 0.35, type: 'sawtooth', volume: 0.08 }]
};

/**
 * Game sounds (Settings → Game sounds). Uses the Web Audio API. Browsers only allow sound after the user
 * has interacted with the page, which is always true here: sounds follow the player's own bet.
 * Only the game and the settings page use it, so it is not part of the first download.
 */
@Injectable({ providedIn: 'root' })
export class SoundService {
  private readonly preferences = inject(PreferencesStore);

  private context: AudioContext | null = null;

  play(sound: GameSound): void {
    if (!this.preferences.soundEnabled()) {
      return;
    }
    const ctx = this.audio();
    if (!ctx) {
      return;
    }
    const start = ctx.currentTime + 0.01;
    for (const note of SOUNDS[sound]) {
      const oscillator = ctx.createOscillator();
      const gain = ctx.createGain();
      oscillator.type = note.type;
      oscillator.frequency.setValueAtTime(note.frequency, start + note.at);
      if (sound === 'crash') {
        oscillator.frequency.exponentialRampToValueAtTime(70, start + note.at + note.duration);
      }
      // Quick fade in and out, so there is no click at the edges
      gain.gain.setValueAtTime(0, start + note.at);
      gain.gain.linearRampToValueAtTime(note.volume, start + note.at + 0.01);
      gain.gain.exponentialRampToValueAtTime(0.0001, start + note.at + note.duration);
      oscillator.connect(gain).connect(ctx.destination);
      oscillator.start(start + note.at);
      oscillator.stop(start + note.at + note.duration + 0.02);
    }
  }

  private audio(): AudioContext | null {
    try {
      this.context ??= new AudioContext();
      if (this.context.state === 'suspended') {
        void this.context.resume();
      }
      return this.context;
    } catch {
      return null; // no Web Audio (very old browser or a test environment)
    }
  }
}
