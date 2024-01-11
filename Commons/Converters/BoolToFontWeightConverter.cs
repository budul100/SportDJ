using Avalonia.Media;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class BoolToFontWeightConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value is bool boolean && boolean
                ? FontWeight.Bold
                : FontWeight.Regular;

            return result;
        }

        #endregion Public Methods
    }
}