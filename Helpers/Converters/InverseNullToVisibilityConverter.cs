using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace OneDesk.Helpers.Converters;

/// <summary>
/// 反向 Null 到 Visibility 转换器（null 或空集合时显示，非 null 时隐藏）
/// </summary>
public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            // 当值为 null 或空集合时显示
            null => Visibility.Visible,
            ICollection collection => collection.Count == 0 ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
