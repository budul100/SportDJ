using Avalonia.Controls;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters;

public class BoolToGridSizeStarConverter
    : OneWayValueConverter
{
    #region Public Methods

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Null or unset values must not throw, they are treated as "hidden"
        var isVisible = value is true;

        var size = isVisible
            ? 1d
            : 0d;

        var type = isVisible
            ? GridUnitType.Star
            : GridUnitType.Pixel;

        var result = new GridLength(
            value: size,
            type: type);

        return result;
    }

    #endregion Public Methods
}
