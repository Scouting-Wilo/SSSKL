using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Domain.Auth;

namespace SSSKL.Mobile.Core.Auth;

/// <summary>
/// The platform's passkey sheet (iOS AuthenticationServices, Android Credential Manager).
/// </summary>
public interface IPasskeyAuthenticator
{
    /// <summary>Whether this device can sign in with a passkey (iOS 16+, Android 9+).</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Asks the user for a passkey. <paramref name="requestOptionsJson"/> is the WebAuthn
    /// <c>PublicKeyCredentialRequestOptions</c> JSON from the API; returns the assertion as WebAuthn
    /// <c>PublicKeyCredential</c> JSON (base64url fields), the shape the SPA posts.
    /// Throws <see cref="PasskeyCanceledException"/> when the user closed the sheet,
    /// <see cref="PasskeyUnavailableException"/> when the device has no passkey for this site and
    /// <see cref="PasskeyPlatformException"/> for any other failure of the platform's passkey API.
    /// </summary>
    Task<string> GetAssertionAsync(string requestOptionsJson, CancellationToken cancellationToken);
}

/// <summary>The user closed the passkey sheet.</summary>
public sealed class PasskeyCanceledException(string? message = null, Exception? inner = null)
    : Exception(message ?? "The passkey request was canceled.", inner);

/// <summary>There is no passkey for this site on the device (or no passkey support at all).</summary>
public sealed class PasskeyUnavailableException(string? message = null, Exception? inner = null)
    : Exception(message ?? "No passkey is available.", inner);

/// <summary>The platform's passkey API failed for another reason (wraps the platform's own exception).</summary>
public sealed class PasskeyPlatformException(string? message = null, Exception? inner = null)
    : Exception(message ?? "The passkey request failed on the device.", inner);

public enum PasskeySignInStatus
{
    Success,
    /// <summary>The user closed the sheet: nothing to report.</summary>
    Canceled,
    /// <summary>No passkey for SSSKL on this device.</summary>
    NoPasskey,
    /// <summary>The API didn't accept the passkey (unknown, removed, account locked or unconfirmed).</summary>
    Rejected,
    /// <summary>Offline, a server error or a platform failure.</summary>
    Failed,
}

public sealed record PasskeySignInResult(PasskeySignInStatus Status, TokenSet? Tokens = null);

/// <summary>
/// The API's passkey endpoints (implemented in Infrastructure). One sign-in attempt runs in one
/// <see cref="IPasskeyApiSession"/>: the API keeps the challenge in a cookie between its two calls.
/// </summary>
public interface IPasskeyApi
{
    IPasskeyApiSession StartSession();
}

public interface IPasskeyApiSession : IDisposable
{
    /// <summary><c>GET identity/PasskeyRequestOptions</c> without a user name: WebAuthn request options JSON.</summary>
    Task<string> GetRequestOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <c>POST identity/PasskeyAssertion</c>: the bearer tokens, or null when the API rejects the passkey.
    /// Throws <see cref="HttpRequestException"/> when offline or on a server error.
    /// </summary>
    Task<TokenSet?> SubmitAssertionAsync(string assertionJson, CancellationToken cancellationToken);
}

/// <summary>
/// Signs in with a passkey, the way the SPA does but without a user name (a discoverable passkey: the API
/// finds the user from it): request options from the API → the platform's passkey sheet → the assertion back
/// to the API, which answers with bearer tokens.
/// </summary>
public sealed class PasskeySignIn(IPasskeyApi api, IPasskeyAuthenticator authenticator, ILogger<PasskeySignIn> logger)
{
    public bool IsSupported => authenticator.IsSupported;

    public async Task<PasskeySignInResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!authenticator.IsSupported)
        {
            return new PasskeySignInResult(PasskeySignInStatus.NoPasskey);
        }

        using var session = api.StartSession();
        try
        {
            var options = await session.GetRequestOptionsAsync(cancellationToken);
            var assertion = await authenticator.GetAssertionAsync(options, cancellationToken);
            var tokens = await session.SubmitAssertionAsync(assertion, cancellationToken);
            if (tokens is null)
            {
                logger.LogWarning("The API rejected the passkey");
                return new PasskeySignInResult(PasskeySignInStatus.Rejected);
            }
            return new PasskeySignInResult(PasskeySignInStatus.Success, tokens);
        }
        catch (PasskeyCanceledException)
        {
            return new PasskeySignInResult(PasskeySignInStatus.Canceled);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new PasskeySignInResult(PasskeySignInStatus.Canceled);
        }
        catch (PasskeyUnavailableException ex)
        {
            logger.LogInformation(ex, "No passkey available");
            return new PasskeySignInResult(PasskeySignInStatus.NoPasskey);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Passkey sign-in failed");
            return new PasskeySignInResult(PasskeySignInStatus.Failed);
        }
        catch (PasskeyPlatformException ex)
        {
            // A platform failure of the passkey sheet: report it, but show the user a normal error.
            logger.LogError(ex, "Passkey sign-in failed on the device");
            return new PasskeySignInResult(PasskeySignInStatus.Failed);
        }
    }
}
