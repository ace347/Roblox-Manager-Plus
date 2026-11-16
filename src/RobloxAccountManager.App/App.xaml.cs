using System.IO;
using System.Security;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RobloxAccountManager.App.ViewModels;
using RobloxAccountManager.Application.Services;
using RobloxAccountManager.Core.Interfaces;
using RobloxAccountManager.Infrastructure.Database;
using RobloxAccountManager.Infrastructure.Logging;
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
            .Enrich.With<SensitiveDataMaskingEnricher>()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var dbInit = _serviceProvider.GetRequiredService<DatabaseInitializer>();
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainVm.DashboardVm.IsLoading = true;
        mainWindow.Show();

        try
        {
            await dbInit.InitializeAsync();
            await _serviceProvider.GetRequiredService<IClientSessionService>().RefreshAsync();
            await mainVm.DashboardVm.RefreshAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Database and session initialization failed ({ExceptionType}).", ex.GetType().Name);
            mainVm.DashboardVm.ErrorMessage = "The local account database could not be initialized. Check that the vault folder is accessible.";
        }
        finally
        {
            mainVm.DashboardVm.IsLoading = false;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxAccountManager", "vault.db");
        services.AddSingleton(sp => new DatabaseInitializer(dbPath, sp.GetRequiredService<ILogger<DatabaseInitializer>>()));

        services.AddSingleton<IAccountRepository, SqliteAccountRepository>();
        services.AddSingleton<IClientSessionRepository, SqliteClientSessionRepository>();
        services.AddSingleton<IClientProcessInspector, RobloxProcessInspector>();
        services.AddSingleton<IEncryptionService, DpapiEncryptionService>();
        services.AddSingleton<ICredentialVault, CredentialVault>();
        services.AddSingleton<IAuthenticodeVerifier, WinTrustAuthenticodeVerifier>();
        services.AddSingleton<RobloxClientLocator>();
        services.AddSingleton<IRobloxLauncher, RobloxProcessLauncher>();
        services.AddSingleton<IClientSessionService, ClientSessionService>();
        services.AddScoped<IAccountService, AccountService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<SessionsViewModel>();
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
