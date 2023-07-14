using System.Windows.Data;
using System.Windows.Media;

namespace SuperCom.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return Brushes.Black;
            SolidColorBrush solidColorBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString(value.ToString()));
            return solidColorBrush;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
