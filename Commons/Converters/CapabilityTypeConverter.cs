using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Converters
{
    public class CapabilityTypeConverter
        : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceType type)
            {
                return type.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion Public Methods
    }
}