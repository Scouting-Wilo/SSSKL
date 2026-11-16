import { detectMobilePlatform } from './mobile-platform';

const nav = (userAgent: string, maxTouchPoints = 0) => ({ userAgent, maxTouchPoints });

describe('detectMobilePlatform', () => {
  it('detects iPhone', () => {
    expect(
      detectMobilePlatform(nav('Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1', 5)),
    ).toBe('ios');
  });

  it('detects iPadOS, which reports a Mac with a touch screen', () => {
    expect(detectMobilePlatform(nav('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Safari/605.1.15', 5))).toBe('ios');
  });

  it('detects Android', () => {
    expect(detectMobilePlatform(nav('Mozilla/5.0 (Linux; Android 15; Pixel 9) AppleWebKit/537.36 Chrome/130.0 Mobile Safari/537.36', 5))).toBe('android');
  });

  it('is null on a desktop Mac', () => {
    expect(detectMobilePlatform(nav('Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 Safari/605.1.15'))).toBeNull();
  });

  it('is null on Windows', () => {
    expect(detectMobilePlatform(nav('Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0 Safari/537.36'))).toBeNull();
  });

  it('is null without a navigator', () => {
    expect(detectMobilePlatform(null)).toBeNull();
  });
});
