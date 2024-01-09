using Avalonia.Controls;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class BoolToGridSizeAutoConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var size = ((bool)value)
                ? 1d
                : 0d;

            var result = new GridLength(
                value: size,
                type: GridUnitType.Auto);

            return result;
        }

        #endregion Public Methods
    }
}