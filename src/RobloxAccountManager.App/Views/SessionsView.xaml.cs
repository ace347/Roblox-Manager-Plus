using System.Windows.Controls;
using System.Windows.Threading;

namespace RobloxAccountManager.App.Views;

public partial class SessionsView : UserControl
{
    private readonly DispatcherTimer _refreshTimer;

    public SessionsView()
    {
        InitializeComponent();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _refreshTimer.Tick += async (_, _) => await RefreshSessionsAsync();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Eagerly populate accounts list as soon as the view appears (don't wait for first timer tick)
        await RefreshSessionsAsync();
        _refreshTimer.Start();
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e) => _refreshTimer.Stop();

    private Task RefreshSessionsAsync() =>
        DataContext is ViewModels.SessionsViewModel viewModel
            ? viewModel.RefreshAsync()
            : Task.CompletedTask;
}
