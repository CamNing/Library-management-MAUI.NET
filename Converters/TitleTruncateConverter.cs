using System.Globalization;
using Microsoft.Maui.Controls;

namespace book.Converters
{
    public class TitleTruncateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string title && !string.IsNullOrEmpty(title))
            {
                const int maxLength = 24;
                if (title.Length > maxLength)
                {
                    return title.Substring(0, maxLength) + "...";
                }
                return title;
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


