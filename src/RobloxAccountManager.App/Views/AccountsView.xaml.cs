using System.Windows;
using System.Windows.Controls;
using RobloxAccountManager.App.ViewModels;

namespace RobloxAccountManager.App.Views;

public partial class AccountsView : UserControl
{
    public AccountsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm)
            vm.RequestRobloxLogin += OpenRobloxLoginWindowAsync;
    }

    /// <summary>
    /// Opens the embedded WebView2 Roblox login window and returns the
    /// captured cookie + username + displayName, or null if the user cancelled.
    /// </summary>
    private Task<(string Cookie, string Username, string DisplayName)?> OpenRobloxLoginWindowAsync()
    {
        var loginWindow = new RobloxLoginWindow
        {
            Owner = Window.GetWindow(this)
        };

        var ok = loginWindow.ShowDialog();
        if (ok != true
            || string.IsNullOrWhiteSpace(loginWindow.CapturedCookie)
            || string.IsNullOrWhiteSpace(loginWindow.CapturedUsername))
        {
            return Task.FromResult<(string Cookie, string Username, string DisplayName)?>(null);
        }

        var displayName = string.IsNullOrWhiteSpace(loginWindow.CapturedDisplayName)
            ? loginWindow.CapturedUsername
            : loginWindow.CapturedDisplayName;

        return Task.FromResult<(string Cookie, string Username, string DisplayName)?>((
            loginWindow.CapturedCookie,
            loginWindow.CapturedUsername,
            displayName
        ));
    }
}
