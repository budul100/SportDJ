using Avalonia.Media;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class BoolToFontStyleConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value is bool boolean && boolean
                ? FontStyle.Italic
                : FontStyle.Normal;

            return result;
        }

        #endregion Public Methods
    }
}