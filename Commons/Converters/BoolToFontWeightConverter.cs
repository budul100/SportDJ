using System;
using System.Globalization;
using Avalonia.Media;

namespace SportDJ.Commons.Converters;

public class BoolToFontWeightConverter
    : OneWayValueConverter
{
    #region Public Methods

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = FontWeight.Regular;

        if (value is bool boolean
            && boolean
            && parameter is FontWeight weight)
        {
            result = weight;
        }

        return result;
    }

    #endregion Public Methods
}