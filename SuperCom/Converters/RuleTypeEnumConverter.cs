using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.Converters
{
    public class RuleTypeEnumConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0;
            Enum.TryParse(value.ToString(), out RuleType ruleType);
            return (int)ruleType;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return RuleType.KeyWord;
            int.TryParse(value.ToString(), out int val);
            return (RuleType)val;
        }
    }
}
