using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RobloxAccountManager.App.ViewModels;

namespace RobloxAccountManager.App.Views;

public partial class AccountsView : UserControl
{
    private readonly DispatcherTimer _refreshTimer;

    public AccountsView()
    {
        InitializeComponent();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _refreshTimer.Tick += async (_, _) =>
        {
            if (DataContext is AccountsViewModel vm)
            {
                await vm.RefreshRunningSessionsAsync();
            }
        };
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm)
        {
            vm.RequestRobloxLogin -= OpenRobloxLoginWindowAsync;
            vm.RequestRobloxLogin += OpenRobloxLoginWindowAsync;
            await vm.RefreshRunningSessionsAsync();
        }
        _refreshTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Stop();
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
