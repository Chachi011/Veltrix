// The SAME formulas as the server (Veltrix.Domain/Crash). Used for:
//  - smooth 60 fps animation between the server's 100 ms ticks
//  - verifying a round's fairness in the browser, without trusting the server

/** m(t) = e^(k·t), t in milliseconds. */
export const GROWTH_PER_MS = 0.00006;
export const HOUSE_EDGE = 0.03;
export const SALT = 'VELTRIX';
export const MAX_CRASH_POINT = 1_000_000;

/** Multiplier after `elapsedMs`, rounded DOWN to 2 decimals (exactly like the server). */
export function multiplierAt(elapsedMs: number): number {
  if (elapsedMs <= 0) {
    return 1;
  }
  return Math.min(Math.floor(Math.exp(GROWTH_PER_MS * elapsedMs) * 100) / 100, MAX_CRASH_POINT);
}

/** Inverse of multiplierAt: milliseconds needed to reach `multiplier`. */
export function elapsedFor(multiplier: number): number {
  return multiplier <= 1 ? 0 : Math.log(multiplier) / GROWTH_PER_MS;
}

/** Unrounded curve value, for drawing a smooth line. */
export function curveAt(elapsedMs: number): number {
  return Math.exp(GROWTH_PER_MS * Math.max(0, elapsedMs));
}

const encoder = new TextEncoder();

function toHex(buffer: ArrayBuffer): string {
  return Array.from(new Uint8Array(buffer), (b) => b.toString(16).padStart(2, '0')).join('');
}

/** SHA-256 in the browser (Web Crypto API). Must equal the hash published before the round. */
export async function sha256Hex(text: string): Promise<string> {
  return toHex(await crypto.subtle.digest('SHA-256', encoder.encode(text)));
}

/**
 * Crash point from a revealed seed, step by step like ProvablyFair.CalculateCrashPoint on the server:
 * h = first 52 bits of HMAC-SHA256(key = seed, message = "VELTRIX"), r = h / 2^52,
 * crash = max(1, floor(100 · (1 − 0.03) / (1 − r)) / 100).
 */
export async function crashPointFromSeed(serverSeed: string): Promise<number> {
  const key = await crypto.subtle.importKey('raw', encoder.encode(serverSeed), { name: 'HMAC', hash: 'SHA-256' }, false, [
    'sign'
  ]);
  const hmac = toHex(await crypto.subtle.sign('HMAC', key, encoder.encode(SALT)));

  // 13 hex digits = 52 bits: still exact in a JavaScript number (53-bit mantissa)
  const h = parseInt(hmac.slice(0, 13), 16);
  const r = h / 2 ** 52;

  // (1 - HOUSE_EDGE) on purpose, not the literal 0.97: it is the exact same double the server computes
  const crash = Math.floor(((1 - HOUSE_EDGE) / (1 - r)) * 100) / 100;

  if (crash < 1) {
    return 1;
  }
  return Math.min(crash, MAX_CRASH_POINT);
}
