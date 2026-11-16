using System.Net.Http.Json;
using System.Net;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Domain.Auth;
using SSSKL.Mobile.Infrastructure.Serialization;

namespace SSSKL.Mobile.Infrastructure.Auth;

/// <summary>Calls <c>POST /api/v1/identity/refresh</c> on an HttpClient without <see cref="AuthTokenHandler"/>.</summary>
public sealed class IdentityTokenRefresher(HttpClient httpClient) : ITokenRefresher
{
    public async Task<TokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/identity/refresh", new RefreshRequest(refreshToken), ApiJsonContext.Default.RefreshRequest, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AccessTokenResponse, cancellationToken);
        return body is null ? null : new TokenSet(body.AccessToken, body.RefreshToken);
    }
}
