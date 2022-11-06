using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SuperCom.Converters
{
    public class ComboBoxSelectedIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            if (values == null || values.Length != 2 || values[0] == null || values[1] == null) return 0;
            string value = values[0].ToString();
            IEnumerable list = values[1] as IEnumerable;

            if (list == null) return 0;
            int idx = 0;
            foreach (var item in list)
            {
                if (item.ToString().ToLower().Equals(value.ToLower()))
                {
                    return idx;
                }
                idx++;
            }
            return 0;
        }

        public object[] ConvertBack(
            object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
