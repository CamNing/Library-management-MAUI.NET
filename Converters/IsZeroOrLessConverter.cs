using System.Globalization;
using Microsoft.Maui.Controls;

namespace book.Converters
{
    public class IsZeroOrLessConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue <= 0;
            }
            if (value is long longValue)
            {
                return longValue <= 0;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

