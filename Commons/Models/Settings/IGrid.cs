using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public interface IGrid
    {
        #region Public Properties

        public int Columns { get; }

        public List<Cell> Contents { get; }

        public int Rows { get; }

        #endregion Public Properties
    }
}