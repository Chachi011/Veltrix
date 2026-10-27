import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PreferencesStore } from '../state/preferences.store';
import { LayoutService } from './layout.service';
import { SoundService } from './sound.service';

/** A Web Audio stand-in that records which notes were started. */
function fakeAudioContext() {
  const param = () => jasmine.createSpyObj('AudioParam', ['setValueAtTime', 'linearRampToValueAtTime', 'exponentialRampToValueAtTime']);
  const started: number[] = [];
  const context = {
    currentTime: 0,
    state: 'running',
    destination: {},
    resume: jasmine.createSpy('resume').and.resolveTo(),
    createGain: () => ({ gain: param(), connect: () => context.destination }),
    createOscillator: () => {
      const oscillator = {
        type: 'sine',
        frequency: param(),
        connect: (node: unknown) => node,
        start: (at: number) => started.push(at),
        stop: jasmine.createSpy('stop')
      };
      return oscillator;
    }
  };
  return { context, started };
}

describe('SoundService (Settings → Game sounds)', () => {
  const soundEnabled = signal(false);
  let audio: ReturnType<typeof fakeAudioContext>;
  let constructed: jasmine.Spy;

  beforeEach(() => {
    soundEnabled.set(false);
    audio = fakeAudioContext();
    constructed = spyOn(window, 'AudioContext').and.returnValue(audio.context as unknown as AudioContext);
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: PreferencesStore, useValue: { soundEnabled } }]
    });
  });

  it('stays silent (and creates no audio context) while sounds are off', () => {
    TestBed.inject(SoundService).play('cashout');
    expect(constructed).not.toHaveBeenCalled();
  });

  it('plays the notes of a sound when sounds are on, reusing one audio context', () => {
    soundEnabled.set(true);
    const sounds = TestBed.inject(SoundService);

    sounds.play('cashout');
    expect(audio.started.length).toBe(3); // the rising three-note chime

    sounds.play('bet');
    expect(audio.started.length).toBe(5);
    expect(constructed).toHaveBeenCalledTimes(1);
  });
});

describe('LayoutService (sidebar)', () => {
  const key = 'vx.sidebarCollapsed';

  beforeEach(() => {
    localStorage.removeItem(key);
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
  });

  afterEach(() => localStorage.removeItem(key));

  it('remembers a collapsed sidebar for the next visit', () => {
    const layout = TestBed.inject(LayoutService);
    expect(layout.sidebarCollapsed()).toBeFalse();

    layout.toggleSidebar();
    TestBed.tick();

    expect(layout.sidebarCollapsed()).toBeTrue();
    expect(localStorage.getItem(key)).toBe('1');
  });

  it('starts collapsed when it was collapsed last time', () => {
    localStorage.setItem(key, '1');
    expect(TestBed.inject(LayoutService).sidebarCollapsed()).toBeTrue();
  });
});
