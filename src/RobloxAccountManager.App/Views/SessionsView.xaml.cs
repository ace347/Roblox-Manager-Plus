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

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) => _refreshTimer.Start();

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e) => _refreshTimer.Stop();

    private Task RefreshSessionsAsync() =>
        DataContext is ViewModels.SessionsViewModel viewModel
            ? viewModel.RefreshAsync()
            : Task.CompletedTask;
}