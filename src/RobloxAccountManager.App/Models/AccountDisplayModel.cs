using CommunityToolkit.Mvvm.ComponentModel;

namespace RobloxAccountManager.App.Models;

public sealed partial class AccountDisplayModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _tag = "Main";

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private string _authenticationStatus = "Sign-in status not verified";

    [ObservableProperty]
    private string _lastActive = "Never";

    public string AvatarPlaceholder { get; init; } = "👤";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int? _runningProcessId;

    [ObservableProperty]
    private Guid? _sessionId;

    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName) || DisplayName == Username
            ? Username
            : $"{Username} ({DisplayName})";
}
