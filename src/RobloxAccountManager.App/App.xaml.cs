using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Infrastructure.Database;
using RobloxAccountManager.Infrastructure.RobloxIntegration;
using RobloxAccountManager.Infrastructure.Security;
using Serilog;
using WpfApp = System.Windows.Application;

namespace RobloxAccountManager.App;

public partial class App : WpfApp
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxAccountManager", "logs", "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize SQLite
        var dbInit = _serviceProvider.GetRequiredService<DatabaseInitializer>();
        await dbInit.InitializeAsync();

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        await mainVm.DashboardVm.RefreshAsync();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxAccountManager", "vault.db");
        services.AddSingleton(sp => new DatabaseInitializer(dbPath, sp.GetRequiredService<ILogger<DatabaseInitializer>>()));

        services.AddSingleton<IAccountRepository, SqliteAccountRepository>();
        services.AddSingleton<IEncryptionService, DpapiEncryptionService>();
        services.AddSingleton<ICredentialVault, CredentialVault>();
        services.AddSingleton<IAuthenticodeVerifier, WinTrustAuthenticodeVerifier>();
        services.AddSingleton<RobloxClientLocator>();
        services.AddSingleton<IRobloxLauncher, RobloxProcessLauncher>();
        services.AddScoped<IAccountService, AccountService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
