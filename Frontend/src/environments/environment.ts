export const environment = {
  apiUrl: typeof CUSTOM_API_URL !== 'undefined' ? CUSTOM_API_URL : '',
  primeUiLicenseKey: typeof PRIMEUI_LICENSE_KEY !== 'undefined' ? PRIMEUI_LICENSE_KEY : '',
  // Sentry project ssskl-frontend. The DSN is a public client key, safe to ship in the bundle.
  sentryDsn: 'https://b14b3d87f3abf9e341b966b7f593fe8d@o4512437930426368.ingest.de.sentry.io/4512437954871376',
  // "ssskl-frontend@<app version>", set by ng-env.js; matches the uploaded source maps.
  sentryRelease: typeof SENTRY_RELEASE !== 'undefined' ? SENTRY_RELEASE : undefined
};
