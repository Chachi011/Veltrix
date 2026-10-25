import { crashPointFromSeed, elapsedFor, multiplierAt, sha256Hex } from './crash-math';

/**
 * Real rounds produced by the C# server (seed, published hash, crash point).
 * If the browser computes the same values, the two implementations are provably identical.
 */
const SERVER_ROUNDS = [
  { seed: '1789a7c13b3b0d2b71e37b2a5d9db082485a6f87cd5cda927dec7318f8612f94', hash: 'a7859de6323d5b677f76a84760f16d2f1c1dd1adf695a6add72581f538d21ae1', crash: 1.0 },
  { seed: '652737a8a4b496042bcd91cb9813b5eb190d20b61dec3d8a5f560c4c1c89df1d', hash: '86576365669c485aa73c29de060af0821ce4116ddd8e1e1a934526a5784a2fde', crash: 2.51 },
  { seed: 'e24a2c632bb2ed9b0e317cd93dc34427d1dd02cf6f5f136d2a2a5916bfce2099', hash: '147d82c04db0430d59bea6dcf77cb41354c67393995541952f97ae0e5989bcaa', crash: 2.75 },
  { seed: '080ce0fdadbb171b51aad7518544f7315b4a71aed2559acdcd25d2b474b82421', hash: 'f67f9d3cf35ae125660710a0ecf595275a215c3ab40d3d7d12b704282eec47d7', crash: 5.17 },
  { seed: 'de395c145ea72046d6ab8e67b9b18783cab5424ddb8d2e7eff81c60a83309fe7', hash: 'a889e2be6939592ce619503f034b53313e1db827995070a8985fb4e67138cac9', crash: 7.47 },
  { seed: '673997a480a1986a097dad9533e9ab890d3effec04ff76b082cc2b25592a4489', hash: 'a5deef188735998a463f57e22d6cb15b17faaaa861cb5874a49f0740a47a07e5', crash: 22.18 },
  { seed: 'f220c80d0775b5ed20b20895481de25d498aff2cb7b8fef8c9cdd7962034e834', hash: '588ab6b8bbc4d72bfefbe1d499787585e1478c440bb9455e5f1d59e108df049b', crash: 170.8 }
];

describe('crash-math', () => {
  describe('multiplier curve', () => {
    it('starts at 1.00x', () => {
      expect(multiplierAt(0)).toBe(1);
      expect(multiplierAt(-100)).toBe(1);
    });

    it('reaches 2x after ~11.55 s, like the server', () => {
      expect(multiplierAt(11_500)).toBe(1.99);
      expect(multiplierAt(11_600)).toBe(2.0);
    });

    it('never decreases', () => {
      let previous = 1;
      for (let ms = 0; ms <= 60_000; ms += 50) {
        const current = multiplierAt(ms);
        expect(current).toBeGreaterThanOrEqual(previous);
        previous = current;
      }
    });

    it('elapsedFor is the inverse of multiplierAt', () => {
      for (const m of [1.01, 2, 13.37, 250]) {
        const t = elapsedFor(m);
        expect(multiplierAt(t - 5)).toBeLessThan(m);
        expect(multiplierAt(t + 5)).toBeGreaterThanOrEqual(m);
      }
    });
  });

  describe('provably fair (in the browser)', () => {
    it('sha256Hex matches the official test vector', async () => {
      expect(await sha256Hex('abc')).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad');
    });

    for (const round of SERVER_ROUNDS) {
      it(`reproduces the server's round: seed ${round.seed.slice(0, 8)}… → hash and ${round.crash}x`, async () => {
        expect(await sha256Hex(round.seed)).toBe(round.hash);
        expect(await crashPointFromSeed(round.seed)).toBe(round.crash);
      });
    }

    it('a different seed gives a different hash (a faked seed is detected)', async () => {
      const faked = SERVER_ROUNDS[1].seed.replace(/^6/, '7');
      expect(await sha256Hex(faked)).not.toBe(SERVER_ROUNDS[1].hash);
    });
  });
});
