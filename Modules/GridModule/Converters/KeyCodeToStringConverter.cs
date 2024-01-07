using SharpHook.Native;
using SportDJ.Commons.Converters;
using System;
using System.Globalization;

namespace SportDJ.Modules.GridModule.Converters
{
    public class KeyCodeToStringConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = default(string);

            if (value is KeyCode keyCode
                && !string.IsNullOrWhiteSpace(keyCode.ToString()))
            {
                result = keyCode.ToString()[2..];
            }

            return result;
        }

        #endregion Public Methods
    }
}