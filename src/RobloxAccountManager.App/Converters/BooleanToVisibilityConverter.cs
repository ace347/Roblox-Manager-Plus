using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RobloxAccountManager.App.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flag = false;
        if (value is bool b)
        {
            flag = b;
        }
        else if (value is string s && !string.IsNullOrEmpty(s))
        {
            flag = true;
        }

        if (Invert)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
