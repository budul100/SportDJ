using SportDJ.Commons.Models.Structure;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Interfaces
{
    public interface IGrid
    {
        #region Public Properties

        int Columns { get; }

        List<Element> Contents { get; }

        int Rows { get; }

        #endregion Public Properties
    }
}