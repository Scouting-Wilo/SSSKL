using AuthenticationServices;
using Foundation;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Infrastructure.Auth;
using UIKit;

namespace SSSKL.Mobile.Services;

public sealed partial class PasskeyAuthenticator
{
    private static partial bool IsSupportedPlatform() => OperatingSystem.IsIOSVersionAtLeast(16);

    private static partial async Task<string> GetAssertionPlatformAsync(string requestOptionsJson, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16))
        {
            throw new PasskeyUnavailableException("Passkeys need iOS 16.");
        }

        var options = PasskeyJson.ParseRequestOptions(requestOptionsJson);
        // The relying party (the API's ServerDomain) must be listed in the app's associated domains
        // (webcredentials:, Entitlements.plist) and its apple-app-site-association must list the app.
        using var provider = new ASAuthorizationPlatformPublicKeyCredentialProvider(options.RelyingPartyId);
        using var request = provider.CreateCredentialAssertionRequest(NSData.FromArray(options.Challenge));
        if (options.AllowedCredentialIds.Count > 0)
        {
            request.AllowedCredentials = options.AllowedCredentialIds
                .Select(id => new ASAuthorizationPlatformPublicKeyCredentialDescriptor(NSData.FromArray(id)))
                .ToArray();
        }
        request.UserVerificationPreference = options.UserVerification switch
        {
            "required" => ASAuthorizationPublicKeyCredentialUserVerificationPreference.Required,
            "discouraged" => ASAuthorizationPublicKeyCredentialUserVerificationPreference.Discouraged,
            _ => ASAuthorizationPublicKeyCredentialUserVerificationPreference.Preferred,
        };

        var completion = new TaskCompletionSource<ASAuthorizationPlatformPublicKeyCredentialAssertion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        // The controller only holds weak references to its delegate and presentation provider: keep them alive here.
        var controllerDelegate = new AssertionDelegate(completion);
        var presentation = new PresentationProvider();
        using var controller = new ASAuthorizationController([request]);
        controller.Delegate = controllerDelegate;
        controller.PresentationContextProvider = presentation;

        await using var registration = cancellationToken.Register(() => MainThread.BeginInvokeOnMainThread(controller.Cancel));
        controller.PerformRequests();
        var assertion = await completion.Task;
        GC.KeepAlive(controllerDelegate);
        GC.KeepAlive(presentation);

        return PasskeyJson.SerializeAssertion(
            assertion.CredentialId.ToArray(),
            assertion.RawClientDataJson.ToArray(),
            assertion.RawAuthenticatorData.ToArray(),
            assertion.Signature.ToArray(),
            assertion.UserId?.ToArray(),
            Attachment(assertion));
    }

    private static string? Attachment(ASAuthorizationPlatformPublicKeyCredentialAssertion assertion)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16, 6))
        {
            return null;
        }
        return assertion.Attachment == ASAuthorizationPublicKeyCredentialAttachment.CrossPlatform ? "cross-platform" : "platform";
    }

    private sealed class AssertionDelegate(TaskCompletionSource<ASAuthorizationPlatformPublicKeyCredentialAssertion> completion)
        : ASAuthorizationControllerDelegate
    {
        public override void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
        {
            if (authorization.GetCredential<ASAuthorizationPlatformPublicKeyCredentialAssertion>() is { } assertion)
            {
                completion.TrySetResult(assertion);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException("The passkey sheet returned no passkey assertion."));
            }
        }

        public override void DidComplete(ASAuthorizationController controller, NSError error)
        {
            Exception exception = error.Domain == ASAuthorizationErrorExtensions.GetDomain(ASAuthorizationError.Canceled)
                && error.Code == (nint)(long)ASAuthorizationError.Canceled
                    ? new PasskeyCanceledException(error.LocalizedDescription)
                    : new NSErrorException(error);
            completion.TrySetException(exception);
        }
    }

    private sealed class PresentationProvider : NSObject, IASAuthorizationControllerPresentationContextProviding
    {
        public UIWindow GetPresentationAnchor(ASAuthorizationController controller) =>
            Platform.GetCurrentUIViewController()?.View?.Window
            ?? UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>()
                .SelectMany(scene => scene.Windows).FirstOrDefault(window => window.IsKeyWindow)
            ?? throw new InvalidOperationException("No window to show the passkey sheet on.");
    }
}
