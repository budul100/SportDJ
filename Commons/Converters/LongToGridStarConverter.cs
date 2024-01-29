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
            if (value != default
                && value is long units
                && units >= 0)
            {
                return new GridLength(
                    value: units,
                    type: GridUnitType.Star);
            }
            else
            {
                return new GridLength(
                    value: 1,
                    type: GridUnitType.Auto);
            }
        }

        #endregion Public Methods
    }
}