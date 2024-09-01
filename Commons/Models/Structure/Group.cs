using System.Collections.Generic;

namespace SportDJ.Commons.Models.Structure
{
    public class Group
        : Cell
    {
        #region Public Properties

        public List<Grid> Grids { get; set; } = [];

        #endregion Public Properties
    }
}