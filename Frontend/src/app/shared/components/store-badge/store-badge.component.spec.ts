import { TestBed } from '@angular/core/testing';
import { EmbeddedService } from '../../../core/services/embedded.service';
import { MobilePlatform } from '../../utils/mobile-platform';
import { MOBILE_PLATFORM, STORE_LINKS, StoreBadgeComponent } from './store-badge.component';

const APP_STORE = 'https://apps.apple.com/nl/app/id123';
const PLAY_STORE = 'https://play.google.com/store/apps/details?id=nl.scoutingwilo.ssskl';

function render(platform: MobilePlatform | null, options: { embedded?: boolean; appStore?: string | null } = {}): HTMLElement {
  TestBed.configureTestingModule({
    imports: [StoreBadgeComponent],
    providers: [
      { provide: MOBILE_PLATFORM, useValue: platform },
      { provide: EmbeddedService, useValue: { isEmbedded: options.embedded ?? false } },
      { provide: STORE_LINKS, useValue: { appStore: options.appStore === undefined ? APP_STORE : options.appStore, playStore: PLAY_STORE } },
    ],
  });
  const fixture = TestBed.createComponent(StoreBadgeComponent);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('StoreBadgeComponent', () => {
  it('shows only the App Store badge on iOS', () => {
    const el = render('ios');

    const links = el.querySelectorAll('a');
    expect(links).toHaveLength(1);
    expect(links[0].getAttribute('href')).toBe(APP_STORE);
    expect(el.querySelector('img')?.getAttribute('src')).toContain('app-store-nl.svg');
    expect(el.querySelector('img')?.getAttribute('alt')).toContain('App Store');
  });

  it('shows only the Google Play badge on Android', () => {
    const el = render('android');

    const links = el.querySelectorAll('a');
    expect(links).toHaveLength(1);
    expect(links[0].getAttribute('href')).toBe(PLAY_STORE);
    expect(el.querySelector('img')?.getAttribute('src')).toContain('google-play-nl.png');
    expect(el.querySelector('img')?.getAttribute('alt')).toContain('Google Play');
  });

  it('shows nothing on desktop', () => {
    expect(render(null).querySelector('a')).toBeNull();
  });

  it('shows nothing inside the mobile app', () => {
    expect(render('android', { embedded: true }).querySelector('a')).toBeNull();
  });

  it('shows nothing on iOS while there is no App Store ID', () => {
    expect(render('ios', { appStore: null }).querySelector('a')).toBeNull();
  });
});
