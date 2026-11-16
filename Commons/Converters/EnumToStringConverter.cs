using System;
using System.Globalization;
using System.Text;

namespace SportDJ.Commons.Converters;

public class EnumToStringConverter
    : OneWayValueConverter
{
    #region Public Properties

    public string WordSeparator { get; set; }

    #endregion Public Properties

    #region Public Methods

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum)
        {
            return string.Empty;
        }

        var valueName = Enum.GetName(value.GetType(), value);

        if (string.IsNullOrEmpty(WordSeparator) || valueName == null || valueName.Length <= 1)
            return valueName;

        var sb = new StringBuilder(valueName.Length * 2);
        sb.Append(valueName[0]);

        for (var index = 1; index < valueName.Length; index++)
        {
            // The last character has no successor, so names like "PFL" must not be checked beyond their length
            if (index + 1 < valueName.Length
                && char.IsUpper(valueName, index)
                && char.IsLower(valueName, index + 1))
            {
                sb.Append(WordSeparator);
            }

            sb.Append(valueName[index]);
        }

        return sb.ToString();
    }

    #endregion Public Methods
}
