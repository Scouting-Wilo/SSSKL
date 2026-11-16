using Microsoft.Extensions.DependencyInjection;
using SSSKL.Mobile.Presentation.Services;
using SSSKL.Mobile.Presentation.ViewModels;
using SSSKL.Mobile.Presentation.Web;

namespace SSSKL.Mobile.Presentation;

public static class DependencyInjection
{
    /// <summary>The view models (a new one per page), the notification badge state and the WebView rules.</summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<NotificationInbox>();
        services.AddSingleton<WebNavigationPolicy>();

        services.AddTransient<PosViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<MoreViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<TopUpsViewModel>();
        services.AddTransient<SaldoListViewModel>();
        services.AddTransient<LeaderboardViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<AchievementsViewModel>();
        services.AddTransient<AchievementDetailViewModel>();
        services.AddTransient<UserProfileViewModel>();
        services.AddTransient<CommentsViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<EventDetailViewModel>();
        services.AddTransient<QuotesViewModel>();
        services.AddTransient<QuoteDetailViewModel>();
        services.AddTransient<ChooserViewModel>();
        return services;
    }
}
