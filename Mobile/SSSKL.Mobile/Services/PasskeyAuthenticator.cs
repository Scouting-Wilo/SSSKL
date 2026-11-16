using SSSKL.Mobile.Core.Auth;

namespace SSSKL.Mobile.Services;

/// <summary>
/// The system passkey sheet: AuthenticationServices on iOS (16+), Credential Manager on Android (9+).
/// Both need the site to vouch for the app: <c>/.well-known/apple-app-site-association</c> (webcredentials) and
/// <c>/.well-known/assetlinks.json</c> (get_login_creds), served by the SPA (Frontend/public/.well-known).
/// </summary>
public sealed partial class PasskeyAuthenticator : IPasskeyAuthenticator
{
    public bool IsSupported => IsSupportedPlatform();

    public Task<string> GetAssertionAsync(string requestOptionsJson, CancellationToken cancellationToken) =>
        MainThread.InvokeOnMainThreadAsync(() => GetAssertionPlatformAsync(requestOptionsJson, cancellationToken));

    private static partial bool IsSupportedPlatform();

    private static partial Task<string> GetAssertionPlatformAsync(string requestOptionsJson, CancellationToken cancellationToken);
}
