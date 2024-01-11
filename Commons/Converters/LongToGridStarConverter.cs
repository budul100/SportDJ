using Avalonia.Controls;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class LongToGridStarConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int units
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