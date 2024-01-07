using System.Collections.Generic;

namespace SportDJ.Commons.Models.Structure
{
    public class Group
        : Cell
    {
        #region Public Properties

        public List<Grid> Grids { get; set; } = new List<Grid>();

        #endregion Public Properties
    }
}