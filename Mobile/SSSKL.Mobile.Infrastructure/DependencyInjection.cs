using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Infrastructure.Api;
using SSSKL.Mobile.Infrastructure.Auth;

namespace SSSKL.Mobile.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The API adapters behind Core's <see cref="ISssklApi"/>, token refresh and passkey ports.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppEndpoints endpoints)
    {
        // The refresher gets its own client without AuthTokenHandler (no recursion on 401).
        services.AddHttpClient<ITokenRefresher, IdentityTokenRefresher>(c => c.BaseAddress = endpoints.ApiBaseUrl);

        // Passkey sign-in: a new client per attempt, with its own cookies (the API keeps the challenge in one).
        services.AddSingleton<IPasskeyApi>(_ => new PasskeyApi(() =>
            new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true })
            {
                BaseAddress = endpoints.ApiBaseUrl,
                Timeout = ApiTimeout,
            }));

        services.AddTransient<AuthTokenHandler>();
        services.AddHttpClient<ISssklApi, SssklApiClient>(c =>
            {
                c.BaseAddress = endpoints.ApiBaseUrl;
                c.Timeout = ApiTimeout;
            })
            .AddHttpMessageHandler<AuthTokenHandler>();
        return services;
    }
}
