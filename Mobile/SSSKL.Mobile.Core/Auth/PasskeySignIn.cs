using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;

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
    /// Throws <see cref="PasskeyCanceledException"/> when the user closed the sheet and
    /// <see cref="PasskeyUnavailableException"/> when the device has no passkey for this site.
    /// </summary>
    Task<string> GetAssertionAsync(string requestOptionsJson, CancellationToken cancellationToken);
}

/// <summary>The user closed the passkey sheet.</summary>
public sealed class PasskeyCanceledException(string? message = null, Exception? inner = null)
    : Exception(message ?? "The passkey request was canceled.", inner);

/// <summary>There is no passkey for this site on the device (or no passkey support at all).</summary>
public sealed class PasskeyUnavailableException(string? message = null, Exception? inner = null)
    : Exception(message ?? "No passkey is available.", inner);

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
/// Signs in with a passkey through the API, the way the SPA does but without a user name (a discoverable
/// passkey: the API finds the user from it):
/// <c>GET /api/v1/identity/PasskeyRequestOptions</c> → the platform's passkey sheet →
/// <c>POST /api/v1/identity/PasskeyAssertion</c>, which answers with bearer tokens.
/// The API keeps the challenge in a cookie between the two calls, so both go through one client with
/// its own cookie container: <paramref name="createClient"/> is called once per attempt and the client
/// is disposed afterwards.
/// </summary>
public sealed class PasskeySignIn(Func<HttpClient> createClient, IPasskeyAuthenticator authenticator, ILogger<PasskeySignIn> logger)
{
    public bool IsSupported => authenticator.IsSupported;

    public async Task<PasskeySignInResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!authenticator.IsSupported)
        {
            return new PasskeySignInResult(PasskeySignInStatus.NoPasskey);
        }

        using var client = createClient();
        try
        {
            string options;
            using (var optionsResponse = await client.GetAsync("api/v1/identity/PasskeyRequestOptions", cancellationToken))
            {
                optionsResponse.EnsureSuccessStatusCode();
                options = await optionsResponse.Content.ReadAsStringAsync(cancellationToken);
            }

            var assertion = await authenticator.GetAssertionAsync(options, cancellationToken);

            using var content = new StringContent(assertion, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/identity/PasskeyAssertion", content, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("The API rejected the passkey ({Status})", (int)response.StatusCode);
                return new PasskeySignInResult(PasskeySignInStatus.Rejected);
            }
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AccessTokenResponse, cancellationToken);
            if (body is null || string.IsNullOrEmpty(body.AccessToken) || string.IsNullOrEmpty(body.RefreshToken))
            {
                logger.LogError("Passkey sign-in returned no tokens");
                return new PasskeySignInResult(PasskeySignInStatus.Failed);
            }
            return new PasskeySignInResult(PasskeySignInStatus.Success, new TokenSet(body.AccessToken, body.RefreshToken));
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
        catch (Exception ex)
        {
            // A platform failure of the passkey sheet: report it, but show the user a normal error.
            logger.LogError(ex, "Passkey sign-in failed on the device");
            return new PasskeySignInResult(PasskeySignInStatus.Failed);
        }
    }
}
