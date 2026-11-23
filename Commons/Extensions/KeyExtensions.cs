using SportDJ.Commons.Models.Records;

namespace SportDJ.Commons.Extensions;

public static class KeyExtensions
{
    #region Public Methods

    public static string GetText(this Key key)
    {
        // Unassigned keys carry channel 0 (valid channels start with 1)
        return key.Channel > 0
            ? $"{key.Type} {key.Channel}/{key.Number}"
            : default;
    }

    #endregion Public Methods
}
