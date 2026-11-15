import { newUuid } from './uuid';

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;

describe('newUuid', () => {
  it('makes a version-4 UUID that the server can read as a Guid', () => {
    expect(newUuid()).toMatch(GUID);
  });

  it('makes a different one every time', () => {
    const ids = new Set(Array.from({ length: 200 }, () => newUuid()));
    expect(ids.size).toBe(200);
  });

  it('works without crypto.randomUUID (plain HTTP, e.g. the server reached by its IP address)', () => {
    const randomUUID = Object.getOwnPropertyDescriptor(Crypto.prototype, 'randomUUID')!;
    delete (Crypto.prototype as Partial<Crypto>).randomUUID;
    try {
      expect(newUuid()).toMatch(GUID);
    } finally {
      Object.defineProperty(Crypto.prototype, 'randomUUID', randomUUID);
    }
  });
});
