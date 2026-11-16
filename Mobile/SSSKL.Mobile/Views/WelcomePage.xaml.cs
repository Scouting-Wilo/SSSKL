using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Views;

/// <summary>
/// Shown while signed out: the logo on the brand colour with an "Inloggen" button that opens the SPA login
/// (<see cref="LoginPage"/>) as a closable sheet on top of this page, and "Inloggen met passkey", which signs in
/// natively through the API with the system passkey sheet (<see cref="PasskeySignIn"/>).
/// </summary>
public partial class WelcomePage : ContentPage
{
    private static readonly Uri AuthorUrl = new("https://ricardotill.nl");

    private readonly IServiceProvider _services;
    private readonly PasskeySignIn _passkeys;
    private bool _opening;

    public WelcomePage(IServiceProvider services, PasskeySignIn passkeys)
    {
        InitializeComponent();
        _services = services;
        _passkeys = passkeys;
        VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
        PasskeyButton.IsVisible = passkeys.IsSupported;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrandSystemBars.Set(true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BrandSystemBars.Set(false);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // A double tap must not stack a second login page.
        if (_opening)
        {
            return;
        }
        _opening = true;
        try
        {
            // A new page per tap: a sheet dismissed by swiping down leaves nothing behind to reuse.
            await Navigation.PushModalAsync(_services.GetRequiredService<LoginPage>(), animated: true);
        }
        finally
        {
            _opening = false;
        }
    }

    private async void OnPasskeyClicked(object? sender, EventArgs e)
    {
        if (_opening)
        {
            return;
        }
        _opening = true;
        SetPasskeyBusy(true);
        try
        {
            var result = await _passkeys.SignInAsync();
            var message = result.Status switch
            {
                PasskeySignInStatus.Success => null,
                PasskeySignInStatus.Canceled => null,
                PasskeySignInStatus.NoPasskey => Texts.PasskeyNotFound,
                PasskeySignInStatus.Rejected => Texts.PasskeyRejected,
                _ => Texts.PasskeyFailed,
            };
            if (result is { Status: PasskeySignInStatus.Success, Tokens: { } tokens })
            {
                await _services.GetRequiredService<AppNavigator>().CompleteSignInAsync(tokens);
            }
            else if (message is not null)
            {
                await _services.GetRequiredService<IUserDialogs>().AlertAsync(Texts.LoginWithPasskey, message);
            }
        }
        finally
        {
            SetPasskeyBusy(false);
            _opening = false;
        }
    }

    private void SetPasskeyBusy(bool busy)
    {
        PasskeyButton.IsVisible = !busy && _passkeys.IsSupported;
        PasskeyBusy.IsVisible = busy;
        PasskeyBusy.IsRunning = busy;
        LoginButton.IsEnabled = !busy;
    }

    private async void OnAuthorTapped(object? sender, TappedEventArgs e) =>
        await Launcher.Default.TryOpenAsync(AuthorUrl);
}
