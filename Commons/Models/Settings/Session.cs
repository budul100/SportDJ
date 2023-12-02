using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public List<Position> Positions { get; set; } = new List<Position>();

        #endregion Public Properties
    }
}