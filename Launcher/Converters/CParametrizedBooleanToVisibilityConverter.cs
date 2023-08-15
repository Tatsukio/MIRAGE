using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MIRAGE_Launcher.Converters
{
    public sealed class CParametrizedBooleanToVisibilityConverter : IValueConverter
    {
        //Visibility="{Binding MyBooleanValue, Converter={StaticResource ParametrizedBooleanToVisibilityConverter}, ConverterParameter=false}"/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;

            if (value is bool boolean)
                flag = boolean;
            else
            {
                if (value is bool?)
                {
                    bool? flag2 = (bool?)value;
                    flag = (flag2.HasValue && flag2.Value);
                }
            }

            //If false is passed as a converter parameter then reverse the value of input value
            if (parameter != null)
            {
                if ((bool.TryParse(parameter.ToString(), out bool par)) && (!par)) flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;

            return false;
        }

        public CParametrizedBooleanToVisibilityConverter()
        {
        }
    }
}
