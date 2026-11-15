/**
 * A random version-4 UUID, e.g. for an idempotency key the server reads as a Guid.
 *
 * Not crypto.randomUUID(): browsers offer it only on HTTPS and localhost, so on the server reached by its IP address
 * over plain HTTP it does not exist. crypto.getRandomValues() works everywhere and is just as random.
 */
export function newUuid(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
  bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant 10xx
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
