/**
 * Where the SSSKL mobile app is published. Used by the store badge on the logged-out home page.
 *
 * `APP_STORE_ID` is the numeric Apple ID of the app (App Store Connect → App Information → Apple ID). While it
 * is empty the App Store badge stays hidden. Keep it in step with the `apple-itunes-app` meta tag in
 * `src/index.html` (Safari's smart app banner), which can't read this file.
 */
export const APP_STORE_ID = '';

/** The Android application ID (Mobile/SSSKL.Mobile.csproj `ApplicationId`). */
export const PLAY_STORE_PACKAGE = 'nl.scoutingwilo.ssskl';

export const APP_STORE_URL = APP_STORE_ID ? `https://apps.apple.com/nl/app/id${APP_STORE_ID}` : null;
export const PLAY_STORE_URL = `https://play.google.com/store/apps/details?id=${PLAY_STORE_PACKAGE}`;
