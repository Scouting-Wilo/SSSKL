using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Domain.Models;
using SSSKL.Mobile.Presentation.ViewModels;
using SSSKL.Mobile.Views.Popups;

namespace SSSKL.Mobile.Services;

/// <summary>Shows popups as transparent modal pages, one at a time.</summary>
public sealed class PopupPresenter(AppEndpoints endpoints) : IPopupPresenter
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Task ShowAchievementsAsync(IReadOnlyList<AchievementEntry> entries) =>
        ShowAsync(() => new AchievementPopupPage(entries
            .Select(e => new AchievementItem(e.AchievementName, e.AchievementDescription, endpoints.ResolveApiUrl(e.ImageUrl), e.Tier, e.DateAdded))
            .ToList()));

    public Task ShowWhatsNewAsync(string html) => ShowAsync(() => new WhatsNewPopupPage(html));

    private async Task ShowAsync(Func<PopupPage> create)
    {
        await _gate.WaitAsync();
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var windows = Application.Current?.Windows;
                var navigation = windows is { Count: > 0 } ? windows[0].Page?.Navigation : null;
                if (navigation is null)
                {
                    return;
                }
                var page = create();
                await navigation.PushModalAsync(page, animated: false);
                await page.Closed;
            });
        }
        finally
        {
            _gate.Release();
        }
    }
}
