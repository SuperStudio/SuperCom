using SuperCom.Entity;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SuperCom.Core.Converters
{
    /// <summary>
    /// 将 null 转换为 Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将 Count=0 转换为 Visibility（用于空状态提示）
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
            {
                // 当 count == 0 时显示提示（Visible）
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 脚本状态颜色转换器
    /// </summary>
    public class ScriptStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ScriptStatus status)
            {
                return status switch
                {
                    ScriptStatus.Waiting => Brushes.Gray,
                    ScriptStatus.Executing => Brushes.DodgerBlue,
                    ScriptStatus.Completed => Brushes.Green,
                    ScriptStatus.Stopped => Brushes.Orange,
                    ScriptStatus.Error => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
