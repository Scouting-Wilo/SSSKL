using Microsoft.Extensions.DependencyInjection;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core;

public static class DependencyInjection
{
    /// <summary>
    /// The application services. Their ports are registered by Infrastructure (API) and the app (platform:
    /// token store, passkey sheet, navigation, dialogs, settings).
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services, AppEndpoints endpoints)
    {
        services.AddSingleton(endpoints);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuthSession>();
        services.AddSingleton<PasskeySignIn>();
        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<ThemePreference>();
        services.AddSingleton<AchievementPopupCoordinator>();
        services.AddSingleton<WhatsNewCoordinator>();
        return services;
    }
}
