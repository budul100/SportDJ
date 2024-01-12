using SharpHook.Native;
using System;
using System.Globalization;

namespace SportDJ.Commons.Converters
{
    public class KeyCodeToStringConverter
        : OneWayValueConverter
    {
        #region Private Fields

        private const string PrefixDefault = "Vc";
        private const string PrefixNumpad = "VcNumPad";

        #endregion Private Fields

        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = default(string);

            if (value is KeyCode keyCode
                && !string.IsNullOrWhiteSpace(keyCode.ToString()))
            {
                if (keyCode.ToString().StartsWith(PrefixNumpad))
                {
                    result = $"[{keyCode.ToString()[8..]}]";
                }
                else if (keyCode.ToString().StartsWith(PrefixDefault))
                {
                    result = $"[{keyCode.ToString()[2..]}]";
                }
                else
                {
                    result = keyCode.ToString();
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}