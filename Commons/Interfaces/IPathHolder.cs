using System;

namespace SportDJ.Commons.Interfaces;

public interface IPathHolder
{
    #region Public Methods

    /// <summary>
    /// Replaces every path stored in the object by the result of the given conversion.
    /// </summary>
    void ConvertPaths(Func<string, string> convert);

    #endregion Public Methods
}
