using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class DoubleToGridPixelConverter
        : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double units
                && units >= 0)
            {
                return new GridLength(
                    value: units,
                    type: GridUnitType.Pixel);
            }
            else
            {
                return new GridLength();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength length
                && length.GridUnitType == GridUnitType.Pixel
                && length.Value >= 0)
            {
                return length.Value;
            }
            else
            {
                return 0;
            }
        }

        #endregion Public Methods
    }
}