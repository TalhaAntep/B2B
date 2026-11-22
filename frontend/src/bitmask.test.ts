import { describe, expect, it } from 'vitest';
import { isOptionEnabled } from './bitmask';

describe('high-order bit display', () => {
  it('keeps values above Number.MAX_SAFE_INTEGER exact', () => {
    const highOption = '4611686018427387904';
    expect(isOptionEnabled(highOption, highOption)).toBe(true);
    expect(isOptionEnabled('4611686018427387905', highOption)).toBe(true);
    expect(isOptionEnabled('1', highOption)).toBe(false);
  });

  it('shows SQL NULL as unset, not as zero', () => {
    expect(isOptionEnabled(null, '1')).toBe(false);
    expect(isOptionEnabled('0', '1')).toBe(false);
  });
});
