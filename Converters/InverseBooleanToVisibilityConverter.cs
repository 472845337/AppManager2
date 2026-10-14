using System;
using System.Globalization;
using System.Windows;

namespace AppManager2.Converters
{
    public class InverseBooleanToVisibilityConverter : BaseValueConverter<InverseBooleanToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new InvalidOperationException("The target must be a System.Windows.Visibility");
            }
            bool boolValue = (bool)value;
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
