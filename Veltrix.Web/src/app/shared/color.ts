/** "#22d3ee" + 0.2 → "rgba(34, 211, 238, 0.2)". Canvas (crash curve, Chart.js) needs concrete colors; tokens are hex values. */
export function withAlpha(hex: string, alpha: number): string {
  const match = /^#([0-9a-f]{6})$/i.exec(hex.trim());
  if (!match) {
    return hex;
  }
  const n = parseInt(match[1], 16);
  return `rgba(${(n >> 16) & 255}, ${(n >> 8) & 255}, ${n & 255}, ${alpha})`;
}
