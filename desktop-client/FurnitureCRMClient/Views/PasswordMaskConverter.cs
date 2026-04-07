using System;
using System.Globalization;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    public class PasswordMaskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string password && !string.IsNullOrEmpty(password))
            {
                return new string('*', password.Length);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
