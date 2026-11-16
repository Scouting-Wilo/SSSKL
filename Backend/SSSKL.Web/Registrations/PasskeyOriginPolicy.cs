using Microsoft.AspNetCore.Identity;

namespace SSSKL.Web.Registrations;

/// <summary>
/// Decides which WebAuthn origins (<c>clientDataJSON.origin</c>) a passkey may come from.
/// Identity's default only accepts the request's <c>Origin</c> header, which works for the SPA but not for
/// the mobile app: its native HttpClient sends no <c>Origin</c>, iOS signs for <c>https://&lt;domain&gt;</c>
/// and Android for <c>android:apk-key-hash:&lt;signing certificate hash&gt;</c>.
/// </summary>
public sealed class PasskeyOriginPolicy
{
    private readonly HashSet<string> _allowedOrigins;

    /// <summary>Allows <c>https://{websiteDomain}</c> plus <paramref name="extraOrigins"/>.</summary>
    /// <param name="websiteDomain">The site's domain; <c>https://{websiteDomain}</c> is always allowed.</param>
    /// <param name="extraOrigins">More origins (<c>Passkeys:AllowedOrigins</c>), e.g. the Android app's.</param>
    public PasskeyOriginPolicy(string websiteDomain, IEnumerable<string>? extraOrigins)
    {
        _allowedOrigins = new HashSet<string>(StringComparer.Ordinal) { $"https://{websiteDomain}" };
        foreach (var origin in (extraOrigins ?? []).Where(origin => !string.IsNullOrWhiteSpace(origin)))
        {
            _allowedOrigins.Add(origin.Trim().TrimEnd('/'));
        }
    }

    public bool IsAllowed(PasskeyOriginValidationContext context)
    {
        // Like Identity's default: no passkeys from a cross-origin iframe.
        if (context.CrossOrigin || string.IsNullOrEmpty(context.Origin))
        {
            return false;
        }

        // Identity's default check: the page that asked for the passkey is the one calling the API (the SPA).
        var requestOrigin = context.HttpContext.Request.Headers.Origin.ToString();
        if (requestOrigin.Length > 0 && string.Equals(requestOrigin, context.Origin, StringComparison.Ordinal))
        {
            return true;
        }

        return _allowedOrigins.Contains(context.Origin);
    }
}
