using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MIRAGE_Launcher.Converters;

/// <summary>true -> Visible, false -> Collapsed. With ConverterParameter=false the other way round.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object p_Value, Type p_TargetType, object p_Parameter, CultureInfo p_Culture)
    {
        bool flag = p_Value is true;
        if ((p_Parameter is string text) && bool.TryParse(text, out bool parameter) && !parameter)
        {
            flag = !flag;
        }
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object p_Value, Type p_TargetType, object p_Parameter, CultureInfo p_Culture) => p_Value is Visibility.Visible;
}