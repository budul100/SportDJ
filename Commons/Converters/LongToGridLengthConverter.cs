using Avalonia.Controls;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class LongToGridLengthConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long units
                && units >= 0)
            {
                return new GridLength(
                    value: units,
                    type: GridUnitType.Star);
            }
            else
            {
                return new GridLength();
            }
        }

        #endregion Public Methods
    }
}