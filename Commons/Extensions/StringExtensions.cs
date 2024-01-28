namespace SportDJ.Commons.Extensions
{
    public static class StringExtensions
    {
        #region Private Fields

        private const string PrefixDefault = "Vc";
        private const string PrefixNumpad = "VcNumPad";

        #endregion Private Fields

        #region Public Methods

        public static string GetKey(this string keyCode)
        {
            var result = default(string);

            if (!string.IsNullOrWhiteSpace(keyCode))
            {
                if (keyCode.StartsWith(PrefixNumpad))
                {
                    result = $"[{keyCode[8..]}]";
                }
                else if (keyCode.StartsWith(PrefixDefault))
                {
                    result = $"[{keyCode[2..]}]";
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
}