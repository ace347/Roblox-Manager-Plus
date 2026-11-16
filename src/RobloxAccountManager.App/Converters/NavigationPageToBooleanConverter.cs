using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RobloxAccountManager.App.ViewModels;

namespace RobloxAccountManager.App.Converters;

public sealed class NavigationPageToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is NavigationPage currentPage && parameter is NavigationPage targetPage && currentPage == targetPage;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && parameter is NavigationPage targetPage ? targetPage : Binding.DoNothing;
}
