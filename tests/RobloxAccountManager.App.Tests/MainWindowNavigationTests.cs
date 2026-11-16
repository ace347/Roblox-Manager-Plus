using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FluentAssertions;
using Moq;
using RobloxAccountManager.App;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.App.Views;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Models;
using Xunit;

namespace RobloxAccountManager.App.Tests;

public sealed class MainWindowNavigationTests
{
    [Fact]
    public void MainWindow_RendersAndSelectsContentForEveryNavigationPage()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            App? application = null;
            MainWindow? window = null;
            StringWriter? bindingOutput = null;
            TextWriterTraceListener? bindingListener = null;
            TraceSource? bindingSource = null;
            var previousTraceLevel = SourceLevels.Off;

            try
            {
                application = new App();
                application.InitializeComponent();
                bindingOutput = new StringWriter();
                bindingListener = new TextWriterTraceListener(bindingOutput);
                bindingSource = PresentationTraceSources.DataBindingSource;
                previousTraceLevel = bindingSource.Switch.Level;
                bindingSource.Switch.Level = SourceLevels.Error;
                bindingSource.Listeners.Add(bindingListener);

                var accountService = new Mock<IAccountService>();
                accountService.Setup(service => service.GetAccountsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Array.Empty<Account>());
                accountService.Setup(service => service.SearchAccountsAsync(
                        It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Array.Empty<Account>());
                var clientSessions = new Mock<IClientSessionService>();
                clientSessions.Setup(service => service.GetRunningPlayerProcessCountAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0);
                clientSessions.Setup(service => service.HasUnmanagedPlayerProcessAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                var testSession = new ClientSession
                {
                    AccountId = Guid.NewGuid(),
                    AccountUsername = "TrackedProfile",
                    AccountDisplayName = "Tracked Display",
                    ProcessId = 1234,
                    ProcessStartedAtUtc = DateTime.UtcNow,
                    ExecutablePath = @"C:\Roblox\RobloxPlayerBeta.exe",
                    Status = ClientSessionStatus.Running
                };
                clientSessions.Setup(service => service.RefreshAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync([testSession]);

                var viewModel = new MainViewModel(
                    new DashboardViewModel(accountService.Object, clientSessions.Object),
                    new AccountsViewModel(accountService.Object),
                    new SessionsViewModel(accountService.Object, clientSessions.Object));
                window = new MainWindow(viewModel)
                {
                    Left = -2000,
                    Top = -2000,
                    Opacity = 0,
                    ShowInTaskbar = false
                };

                window.Show();
                window.UpdateLayout();

                var contentHost = (ContentControl)window.FindName("MainContentHost")!;
                var routes = new (NavigationPage Page, string NavigationName, string ExpectedText)[]
                {
                    (NavigationPage.Dashboard, "DashboardNavigation", "Welcome Back"),
                    (NavigationPage.Accounts, "AccountsNavigation", "Accounts Vault"),
                    (NavigationPage.Sessions, "SessionsNavigation", "Roblox Player Sessions"),
                    (NavigationPage.Activity, "ActivityNavigation", "Activity"),
                    (NavigationPage.Security, "SecurityNavigation", "Security"),
                    (NavigationPage.Settings, "SettingsNavigation", "Settings")
                };

                foreach (var route in routes)
                {
                    viewModel.NavigateCommand.ExecuteAsync(route.Page).GetAwaiter().GetResult();
                    window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, window.UpdateLayout);

                    contentHost.Content.Should().BeSameAs(viewModel.CurrentPageViewModel);
                    ((RadioButton)window.FindName(route.NavigationName)!).IsChecked.Should().BeTrue();
                    FindTextBlock(contentHost, route.ExpectedText).Should().NotBeNull();
                    if (route.Page == NavigationPage.Dashboard)
                    {
                        var dashboard = FindVisual<DashboardView>(contentHost)!;
                        var metricsGrid = FindNamedVisual<Grid>(contentHost, "DashboardMetricsGrid")!;
                        var rightCard = FindNamedVisual<Border>(contentHost, "StoredSecretsCard")!;
                        var columnWidths = string.Join(",", metricsGrid.ColumnDefinitions.Select(column => column.ActualWidth));
                        var cardRightEdge = metricsGrid.ColumnDefinitions.Take(4).Sum(column => column.ActualWidth) + rightCard.ActualWidth;

                        metricsGrid.ActualWidth.Should().BeLessThanOrEqualTo(dashboard.ActualWidth + 1);
                        cardRightEdge.Should().BeLessThanOrEqualTo(
                            metricsGrid.ActualWidth + 1,
                            $"column widths are [{columnWidths}], card width is {rightCard.ActualWidth}, and dashboard width is {dashboard.ActualWidth}");
                        FindTextBlock(contentHost, "No accounts yet. Add a profile to see it here.")!
                            .Visibility.Should().Be(Visibility.Visible);

                        viewModel.DashboardVm.IsLoading = true;
                        window.UpdateLayout();
                        FindNamedVisual<ProgressBar>(contentHost, "DashboardLoadingIndicator")!
                            .Visibility.Should().Be(Visibility.Visible);
                        viewModel.DashboardVm.IsLoading = false;

                        viewModel.DashboardVm.ErrorMessage = "Unable to load dashboard data from the local database.";
                        window.UpdateLayout();
                        FindNamedVisual<Border>(contentHost, "DashboardErrorBanner")!
                            .Visibility.Should().Be(Visibility.Visible);
                        viewModel.DashboardVm.ErrorMessage = null;
                    }
                    else if (route.Page == NavigationPage.Sessions)
                    {
                        FindTextBlock(contentHost, "TrackedProfile").Should().NotBeNull();
                        FindTextBlock(contentHost, "Running").Should().NotBeNull();
                        FindTextBlock(contentHost, "Unverified; check sign-in in Roblox Player").Should().NotBeNull();
                        FindTextBlock(contentHost, testSession.ExecutablePath).Should().NotBeNull();
                    }
                }

                bindingListener.Flush();
                bindingOutput.ToString().Should().BeEmpty("navigation should not produce WPF binding errors");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (bindingListener is not null)
                {
                    bindingSource!.Listeners.Remove(bindingListener);
                    bindingSource.Switch.Level = previousTraceLevel;
                }

                window?.Close();
                application?.Shutdown();
                bindingOutput?.Dispose();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        failure.Should().BeNull("all navigation routes should render their implicit WPF templates");
    }

    private static TextBlock? FindTextBlock(DependencyObject root, string text)
    {
        if (root is TextBlock textBlock && textBlock.Text == text)
        {
            return textBlock;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var result = FindTextBlock(VisualTreeHelper.GetChild(root, index), text);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static T? FindVisual<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T element)
        {
            return element;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var result = FindVisual<T>(VisualTreeHelper.GetChild(root, index));
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static T? FindNamedVisual<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name)
        {
            return element;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var result = FindNamedVisual<T>(VisualTreeHelper.GetChild(root, index), name);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
