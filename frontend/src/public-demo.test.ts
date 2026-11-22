import { describe, expect, it } from 'vitest';
import { allSettings } from './catalog';
import { createPublicDemoItems } from './public-demo';

describe('public demo data', () => {
  it('creates one synthetic, unset, non-persistable item for every catalog setting', () => {
    const items = createPublicDemoItems();
    expect(items).toHaveLength(98);
    expect(items.map((item) => item.parameterId)).toEqual(allSettings.map((setting) => setting.parameterId));
    expect(items.every((item) => item.value === null)).toBe(true);
    expect(items.every((item) => item.editable === false)).toBe(true);
    expect(items.every((item, index) => item.mappingStatus === allSettings[index].mapping.status)).toBe(true);
  });
});
