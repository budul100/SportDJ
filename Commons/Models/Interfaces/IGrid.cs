using SportDJ.Commons.Models.Settings;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Interfaces
{
    public interface IGrid
    {
        #region Public Properties

        int Columns { get; }

        List<Cell> Contents { get; }

        int Rows { get; }

        #endregion Public Properties
    }
}