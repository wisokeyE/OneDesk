using System.Globalization;
using System.Windows.Data;

namespace OneDesk.Helpers.Converters;

internal class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return false;
        }

        if (parameter is not string enumString)
        {
            throw new ArgumentException("参数必须是枚举值名称字符串");
        }

        var type = value.GetType();

        if (!type.IsEnum)
        {
            throw new ArgumentException("值必须是枚举类型");
        }

        var enumValue = Enum.Parse(type, enumString);

        return enumValue.Equals(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("参数必须是枚举值名称字符串");
        }

        if (!targetType.IsEnum)
        {
            throw new ArgumentException("目标类型必须是枚举类型");
        }

        return Enum.Parse(targetType, enumString);
    }
}