using System;
using System.Globalization;
using Avalonia.Controls;

namespace SportDJ.Commons.Converters
{
    public class LongToGridStarConverter
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
                return new GridLength(
                    value: 0,
                    type: GridUnitType.Star);
            }
        }

        #endregion Public Methods
    }
}