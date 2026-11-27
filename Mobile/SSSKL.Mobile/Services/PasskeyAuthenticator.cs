
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

    public async Task<string> GetAssertionAsync(string requestOptionsJson, CancellationToken cancellationToken)
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(() => GetAssertionPlatformAsync(requestOptionsJson, cancellationToken));
        }
        catch (Exception ex) when (ex is not (PasskeyCanceledException or PasskeyUnavailableException or OperationCanceledException))
        {
            // Everything else the platform throws (NSErrorException, Java exceptions, a missing window) is one
            // contract failure for PasskeySignIn, which reports it and shows a normal error.
            throw new PasskeyPlatformException($"The passkey sheet failed: {ex.Message}", ex);
        }
    }

    private static partial bool IsSupportedPlatform();

    private static partial Task<string> GetAssertionPlatformAsync(string requestOptionsJson, CancellationToken cancellationToken);
}
