using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InfoBaseListManager
{
    public class LastTimeToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter,
          CultureInfo culture)
        {
            if ((bool)value)
                return new SolidColorBrush(Colors.Green);
            else
                return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
          CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
        
}
