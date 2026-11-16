using AndroidX.Core.Content;
using AndroidX.Credentials;
using Android.Runtime;
using AndroidX.Credentials.Exceptions;
using SSSKL.Mobile.Core.Auth;

namespace SSSKL.Mobile.Services;

public sealed partial class PasskeyAuthenticator
{
    // Credential Manager supports passkeys from Android 9 (via Google Play services before Android 14).
    private static partial bool IsSupportedPlatform() => OperatingSystem.IsAndroidVersionAtLeast(28);

    private static partial async Task<string> GetAssertionPlatformAsync(string requestOptionsJson, CancellationToken cancellationToken)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No activity to show the passkey sheet on.");

        // Credential Manager takes the WebAuthn request options JSON as is and answers with the assertion JSON.
        // The site's assetlinks.json (get_login_creds) must list this app's signing certificate.
        var request = new GetCredentialRequest.Builder()
            .AddCredentialOption(new GetPublicKeyCredentialOption(requestOptionsJson))
            .Build();

        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var signal = new Android.OS.CancellationSignal();
        await using var registration = cancellationToken.Register(signal.Cancel);

        var executor = ContextCompat.GetMainExecutor(activity)
            ?? throw new InvalidOperationException("No main executor.");
        var manager = CredentialManager.Companion.Create(activity);
        manager.GetCredentialAsync(activity, request, signal, executor, new Callback(completion));
        return await completion.Task;
    }

    private sealed class Callback(TaskCompletionSource<string> completion) : Java.Lang.Object, ICredentialManagerCallback
    {
        public void OnResult(Java.Lang.Object? result)
        {
            // Only a passkey was asked for, so the response holds a PublicKeyCredential.
            var credential = result?.JavaCast<GetCredentialResponse>()?.Credential;
            if (credential is not null && credential.Type == PublicKeyCredential.TypePublicKeyCredential)
            {
                completion.TrySetResult(credential.JavaCast<PublicKeyCredential>()!.AuthenticationResponseJson);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException("Credential Manager returned no passkey."));
            }
        }

        public void OnError(Java.Lang.Object? error)
        {
            // The callback hands the GetCredentialException over as a plain Object (Java exceptions are no
            // Java.Lang.Object in .NET): resolve it to its bound exception type to tell the cases apart.
            var exception = error is null
                ? null
                : Java.Lang.Object.GetObject<Java.Lang.Throwable>(error.Handle, JniHandleOwnership.DoNotTransfer);
            Exception result = exception switch
            {
                GetCredentialCancellationException => new PasskeyCanceledException(exception.Message),
                NoCredentialException => new PasskeyUnavailableException(exception.Message),
                _ => new InvalidOperationException($"Credential Manager failed: {exception?.GetType().Name}: {exception?.Message}", exception),
            };
            completion.TrySetException(result);
        }
    }
}
