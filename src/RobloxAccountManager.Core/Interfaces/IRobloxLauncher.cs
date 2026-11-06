namespace RobloxAccountManager.Core.Interfaces;

public interface IRobloxLauncher
{
    bool IsClientInstalled();
    string? FindClientExecutablePath();
    Task<bool> LaunchAsync(string launchArguments, CancellationToken cancellationToken = default);
}
