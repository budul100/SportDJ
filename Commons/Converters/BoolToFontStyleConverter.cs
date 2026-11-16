using System;
using System.Globalization;
using Avalonia.Media;

namespace SportDJ.Commons.Converters;

public class BoolToFontStyleConverter
    : OneWayValueConverter
{
    #region Public Methods

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = FontStyle.Normal;

        if (value is bool boolean
            && boolean
            && parameter is FontStyle style)
        {
            result = style;
        }

        return result;
    }

    #endregion Public Methods
}