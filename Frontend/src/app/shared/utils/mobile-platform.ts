export type MobilePlatform = 'ios' | 'android';

/**
 * The mobile OS the browser runs on, or null on desktop (and anything else). iPadOS Safari reports itself as a
 * Mac, so a "Macintosh" with a touch screen counts as iOS.
 */
export function detectMobilePlatform(
  navigatorLike: Pick<Navigator, 'userAgent' | 'maxTouchPoints'> | null | undefined = typeof navigator === 'undefined'
    ? undefined
    : navigator,
): MobilePlatform | null {
  if (!navigatorLike) {
    return null;
  }
  const userAgent = navigatorLike.userAgent ?? '';
  if (/Android/i.test(userAgent)) {
    return 'android';
  }
  if (/iPhone|iPad|iPod/i.test(userAgent) || (/Macintosh/i.test(userAgent) && (navigatorLike.maxTouchPoints ?? 0) > 1)) {
    return 'ios';
  }
  return null;
}
