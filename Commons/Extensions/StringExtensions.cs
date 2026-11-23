using System;

namespace SportDJ.Commons.Extensions;

public static class StringExtensions
{
    #region Private Fields

    private const string PrefixNumpad = "NumPad";

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Returns the text of a key code without any decoration, e.g. "1" for "NumPad1".
    /// Whether the key belongs to the numeric keypad is told by <see cref="IsNumpad"/>.
    /// </summary>
    public static string GetKey(this string keyCode)
    {
        var result = default(string);

        if (!string.IsNullOrWhiteSpace(keyCode))
        {
            result = keyCode.IsNumpad()
                ? GetNumpadText(keyCode[PrefixNumpad.Length..])
                : keyCode;
        }

        return result;
    }

    public static bool IsNumpad(this string keyCode)
    {
        return !string.IsNullOrWhiteSpace(keyCode)
            && keyCode.Length > PrefixNumpad.Length
            && keyCode.StartsWith(
                value: PrefixNumpad,
                comparisonType: StringComparison.Ordinal);
    }

    #endregion Public Methods

    #region Private Methods

    private static string GetNumpadText(string name)
    {
        return name switch
        {
            "Add" => "+",
            "Subtract" => "-",
            "Multiply" => "*",
            "Divide" => "/",
            "Decimal" => ".",
            "Comma" => ",",
            "Equal" => "=",
            _ => name,
        };
    }

    #endregion Private Methods
}
