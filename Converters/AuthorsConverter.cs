using System.Globalization;
using Microsoft.Maui.Controls;

namespace book.Converters
{
    public class AuthorsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is List<string> authors && authors.Any())
            {
                return string.Join(", ", authors);
            }
            return "No authors";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

