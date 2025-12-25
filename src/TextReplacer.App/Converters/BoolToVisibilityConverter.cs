using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TextReplacer.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value switch
        {
            bool b => b,
            int i => i > 0,
            _ => false
        };

        // If parameter is "Inverse", invert the logic
        if (parameter is string param && param == "Inverse")
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
