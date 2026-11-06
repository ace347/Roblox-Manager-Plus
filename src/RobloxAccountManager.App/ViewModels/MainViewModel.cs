using CommunityToolkit.Mvvm.ComponentModel;

namespace RobloxAccountManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Secure Roblox Account Manager";

    [ObservableProperty]
    private string _statusMessage = "Ready";
}
