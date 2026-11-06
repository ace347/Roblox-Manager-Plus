using System.Windows;
using RobloxAccountManager.App.ViewModels;

namespace RobloxAccountManager.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
