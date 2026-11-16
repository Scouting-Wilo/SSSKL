using SSSKL.Mobile.Domain.Auth;

namespace SSSKL.Mobile.Core.Auth;

/// <summary>Persists the token pair; implemented with SecureStorage in the app.</summary>
public interface ITokenStore
{
    Task<TokenSet?> GetAsync();
    Task SetAsync(TokenSet tokens);
    Task ClearAsync();
}

/// <summary>Exchanges a refresh token for a new pair; returns null when the refresh token is rejected.</summary>
public interface ITokenRefresher
{
    Task<TokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
