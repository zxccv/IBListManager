using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using InfoBaseListDataClasses;

namespace InfoBaseListManager
{
    public class LastTimeToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter,
          System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return new SolidColorBrush(Colors.Green);
            else
                return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
          System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
        
}
