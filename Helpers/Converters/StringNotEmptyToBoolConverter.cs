using System.Globalization;
using System.Windows.Data;

namespace OneDesk.Helpers.Converters;

/// <summary>
/// 字符串非空到 bool 转换器（非空 → true，空/null → false）
/// </summary>
public class StringNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
