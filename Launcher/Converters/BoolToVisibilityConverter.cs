using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MIRAGE_Launcher.Converters
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool boolean ? boolean : (value as bool?) ?? false;

            if (parameter is string param && bool.TryParse(param, out bool par) && !par)
                flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }
}
