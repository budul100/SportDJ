namespace SportDJ.Commons.Extensions;

public static class StringExtensions
{
    #region Private Fields

    private const string PrefixNumpad = "NumPad";

    #endregion Private Fields

    #region Public Methods

    public static string GetKey(this string keyCode)
    {
        var result = default(string);

        if (!string.IsNullOrWhiteSpace(keyCode))
        {
            if (keyCode.StartsWith(PrefixNumpad))
            {
                result = $"[{keyCode[6..]}]";
            }
            else
            {
                result = keyCode;
            }
        }

        return result;
    }

    #endregion Public Methods
}