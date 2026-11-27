namespace SSSKL.Mobile.Infrastructure.Serialization;

// Request/response bodies that only the API adapters see (the identity endpoints); the app's models are in
// SSSKL.Mobile.Domain.Models.

public sealed record AccessTokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);

public sealed record RefreshRequest(string RefreshToken);

/// <summary>An empty body reads the 2FA status (<c>POST identity/manage/2fa</c>).</summary>
public sealed record TwoFactorRequest;
