using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Presentation.Localization;

namespace SSSKL.Mobile.Services;

public sealed class UserDialogs : IUserDialogs
{
    public Task ToastAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => Toast.Make(message, ToastDuration.Short).Show());

    public Task AlertAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage?.DisplayAlertAsync(title, message, Texts.Ok) ?? Task.CompletedTask);

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage?.DisplayAlertAsync(title, message, accept, cancel) ?? Task.FromResult(false));

    public async Task<string?> ActionSheetAsync(string? title, string cancel, string? destruction, params string[] buttons)
    {
        var choice = await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (CurrentPage is not { } page)
            {
                return null;
            }
            string? picked = await page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
            return picked;
        });
        return choice == cancel ? null : choice;
    }

    private static Page? CurrentPage
    {
        get
        {
            var windows = Application.Current?.Windows;
            var page = windows is { Count: > 0 } ? windows[0].Page : null;
            var modals = page?.Navigation.ModalStack;
            return modals is { Count: > 0 } ? modals[^1] : page;
        }
    }
}
