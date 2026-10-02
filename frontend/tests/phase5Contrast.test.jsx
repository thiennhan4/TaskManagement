import { readFileSync } from 'node:fs';
import { expect, it } from 'vitest';

const luminance = hex => {
  const rgb = hex.match(/[a-f0-9]{2}/gi).map(value => parseInt(value, 16) / 255).map(value => value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4);
  return rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722;
};
it('primary button foreground exceeds 4.5:1 against both theme yellow tokens', () => {
  const css = readFileSync('src/styles/index.css', 'utf8');
  const values = [...css.matchAll(/--color-primary:\s*(#[A-Fa-f0-9]{6})/g)].map(match => match[1]);
  const foreground = css.match(/--color-text-inverse:\s*(#[A-Fa-f0-9]{6})/)[1];
  expect(values).toHaveLength(2);
  for (const value of values) expect((luminance(value) + .05) / (luminance(foreground) + .05)).toBeGreaterThanOrEqual(4.5);
});
