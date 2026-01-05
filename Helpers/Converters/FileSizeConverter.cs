using System.Globalization;
using System.Windows.Data;

namespace OneDesk.Helpers.Converters;

public class FileSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long size) return string.Empty;
        if (size == 0) return "-";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double fileSize = size;
        var unitIndex = 0;

        while (fileSize >= 1024 && unitIndex < units.Length - 1)
        {
            fileSize /= 1024;
            unitIndex++;
        }

        return $"{fileSize:F2} {units[unitIndex]}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
