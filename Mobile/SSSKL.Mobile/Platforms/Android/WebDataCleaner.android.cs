using Android.Webkit;

namespace SSSKL.Mobile.Services;

public sealed partial class WebDataCleaner
{
    private static partial Task ClearPlatformAsync()
    {
        WebStorage.Instance?.DeleteAllData();
        CookieManager.Instance?.RemoveAllCookies(null);
        CookieManager.Instance?.Flush();
        return Task.CompletedTask;
    }
}
