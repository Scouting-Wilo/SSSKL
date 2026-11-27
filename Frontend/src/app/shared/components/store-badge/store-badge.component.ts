import { ChangeDetectionStrategy, Component, DOCUMENT, InjectionToken, inject } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { APP_STORE_URL, PLAY_STORE_URL } from '../../../core/config/app-stores';
import { EmbeddedService } from '../../../core/services/embedded.service';
import { LanguageService } from '../../../core/services/language.service';
import { MobilePlatform, detectMobilePlatform } from '../../utils/mobile-platform';

/** The store pages of the app; a token so tests can set an App Store URL. */
export const STORE_LINKS = new InjectionToken<{ appStore: string | null; playStore: string }>('STORE_LINKS', {
  providedIn: 'root',
  factory: () => ({ appStore: APP_STORE_URL, playStore: PLAY_STORE_URL }),
});

/** The device's mobile OS (null on desktop); a token so tests can pick one. */
export const MOBILE_PLATFORM = new InjectionToken<MobilePlatform | null>('MOBILE_PLATFORM', {
  providedIn: 'root',
  factory: () => detectMobilePlatform(inject(DOCUMENT).defaultView?.navigator),
});

interface StoreBadge {
  url: string;
  image: string;
  width: number;
  height: number;
  alt: string;
}

/**
 * The one store badge that fits the device: App Store on iPhone/iPad, Google Play on Android, nothing on
 * desktop or inside the mobile app itself. The App Store badge stays hidden until `APP_STORE_ID` is set.
 * Official badge artwork (Dutch) in `public/images/badges`; the Google Play image has its own padding, hence
 * its larger box.
 */
@Component({
  selector: 'app-store-badge',
  imports: [NgOptimizedImage],
  template: `
    @if (badge; as badge) {
      <a [href]="badge.url" target="_blank" rel="noopener" class="inline-block">
        <img [ngSrc]="badge.image" [width]="badge.width" [height]="badge.height" [alt]="badge.alt" />
      </a>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StoreBadgeComponent {
  private readonly lang = inject(LanguageService);
  private readonly links = inject(STORE_LINKS);

  readonly badge: StoreBadge | null = this.pick(inject(MOBILE_PLATFORM), inject(EmbeddedService).isEmbedded);

  private pick(platform: MobilePlatform | null, embedded: boolean): StoreBadge | null {
    if (embedded) {
      return null;
    }
    if (platform === 'ios' && this.links.appStore) {
      return {
        url: this.links.appStore,
        image: '/images/badges/app-store-nl.svg',
        width: 120,
        height: 40,
        alt: this.lang.translate('download_app_store'),
      };
    }
    if (platform === 'android') {
      return {
        url: this.links.playStore,
        image: '/images/badges/google-play-nl.png',
        width: 150,
        height: 58,
        alt: this.lang.translate('download_google_play'),
      };
    }
    return null;
  }
}
