using System.Buffers.Text;
using System.Text.Json;

namespace SSSKL.Mobile.Core.Auth;

/// <summary>The parts of the API's WebAuthn request options that a native passkey request needs.</summary>
public sealed record PasskeyRequestOptions(
    byte[] Challenge,
    string RelyingPartyId,
    IReadOnlyList<byte[]> AllowedCredentialIds,
    string? UserVerification);

/// <summary>
/// Converts between WebAuthn JSON (as the API speaks it: binary fields base64url-encoded) and the raw values
/// of the iOS passkey API. Android's Credential Manager takes and returns the JSON itself.
/// Reflection-free (JsonDocument / Utf8JsonWriter) for Native AOT.
/// </summary>
public static class PasskeyJson
{
    /// <summary>Reads <c>PublicKeyCredentialRequestOptions</c>; also accepts it wrapped in <c>{"publicKey": …}</c>.</summary>
    public static PasskeyRequestOptions ParseRequestOptions(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("publicKey", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
        {
            root = wrapped;
        }

        var challenge = Base64Url.DecodeFromChars(root.GetProperty("challenge").GetString());
        var rpId = root.GetProperty("rpId").GetString();
        if (string.IsNullOrEmpty(rpId))
        {
            throw new JsonException("The passkey request options have no rpId.");
        }

        var allowed = new List<byte[]>();
        if (root.TryGetProperty("allowCredentials", out var credentials) && credentials.ValueKind == JsonValueKind.Array)
        {
            foreach (var credential in credentials.EnumerateArray())
            {
                if (credential.TryGetProperty("id", out var id) && id.GetString() is { Length: > 0 } value)
                {
                    allowed.Add(Base64Url.DecodeFromChars(value));
                }
            }
        }

        var userVerification = root.TryGetProperty("userVerification", out var uv) && uv.ValueKind == JsonValueKind.String
            ? uv.GetString()
            : null;

        return new PasskeyRequestOptions(challenge, rpId, allowed, userVerification);
    }

    /// <summary>Builds the assertion (<c>PublicKeyCredential</c> JSON) that <c>PasskeyAssertion</c> expects.</summary>
    public static string SerializeAssertion(
        byte[] credentialId,
        byte[] clientDataJson,
        byte[] authenticatorData,
        byte[] signature,
        byte[]? userHandle,
        string? authenticatorAttachment = "platform")
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            var id = Base64Url.EncodeToString(credentialId);
            writer.WriteStartObject();
            writer.WriteString("id", id);
            writer.WriteString("rawId", id);
            writer.WriteString("type", "public-key");
            writer.WriteStartObject("response");
            writer.WriteString("clientDataJSON", Base64Url.EncodeToString(clientDataJson));
            writer.WriteString("authenticatorData", Base64Url.EncodeToString(authenticatorData));
            writer.WriteString("signature", Base64Url.EncodeToString(signature));
            if (userHandle is { Length: > 0 })
            {
                writer.WriteString("userHandle", Base64Url.EncodeToString(userHandle));
            }
            else
            {
                writer.WriteNull("userHandle");
            }
            writer.WriteEndObject();
            writer.WriteStartObject("clientExtensionResults");
            writer.WriteEndObject();
            if (authenticatorAttachment is null)
            {
                writer.WriteNull("authenticatorAttachment");
            }
            else
            {
                writer.WriteString("authenticatorAttachment", authenticatorAttachment);
            }
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
