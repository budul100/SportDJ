using Avalonia.Media;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters;

public class BoolToColorConverter
    : OneWayValueConverter
{
    #region Public Methods

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = Colors.Transparent;

        if (value is bool boolean
            && boolean
            && parameter is Color color)
        {
            result = color;
        }

        return result;
    }

    #endregion Public Methods
}