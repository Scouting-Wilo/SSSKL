using System.Net.Http.Json;
using System.Net;
using System.Text;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Domain.Auth;
using SSSKL.Mobile.Infrastructure.Serialization;

namespace SSSKL.Mobile.Infrastructure.Auth;

/// <summary>
/// The passkey endpoints of <c>/api/v1/identity</c>. The API keeps the challenge in a cookie between the
/// two calls, so each session uses its own client from <paramref name="createClient"/> (with its own cookie
/// container), disposed with the session.
/// </summary>
public sealed class PasskeyApi(Func<HttpClient> createClient) : IPasskeyApi
{
    public IPasskeyApiSession StartSession() => new Session(createClient());

    private sealed class Session(HttpClient client) : IPasskeyApiSession
    {
        public async Task<string> GetRequestOptionsAsync(CancellationToken cancellationToken)
        {
            using var response = await client.GetAsync("api/v1/identity/PasskeyRequestOptions", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        public async Task<TokenSet?> SubmitAssertionAsync(string assertionJson, CancellationToken cancellationToken)
        {
            using var content = new StringContent(assertionJson, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1/identity/PasskeyAssertion", content, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AccessTokenResponse, cancellationToken);
            if (body is null || string.IsNullOrEmpty(body.AccessToken) || string.IsNullOrEmpty(body.RefreshToken))
            {
                throw new HttpRequestException("Passkey sign-in returned no tokens.");
            }
            return new TokenSet(body.AccessToken, body.RefreshToken);
        }

        public void Dispose() => client.Dispose();
    }
}
