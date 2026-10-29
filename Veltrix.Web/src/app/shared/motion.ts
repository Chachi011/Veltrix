// "Reduce motion": the operating system setting OR the player's own choice in Settings (animations off).
// The Settings choice is the data-motion="reduce" attribute on <html>; styles.css turns animations off for both.

export const MOTION_ATTRIBUTE = 'data-motion';

export function reducedMotion(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }
  return (
    document.documentElement.getAttribute(MOTION_ATTRIBUTE) === 'reduce' ||
    (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false)
  );
}
