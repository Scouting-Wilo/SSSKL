namespace SSSKL.Mobile.Domain.Auth;

/// <summary>An ASP.NET Identity bearer token pair, as issued by <c>/api/v1/identity/login|refresh</c>.</summary>
public sealed record TokenSet(string AccessToken, string RefreshToken);
