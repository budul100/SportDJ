using System.Collections.Generic;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Models.Settings
{
    public class Set
    {
        #region Public Properties

        public Grid Grid { get; set; } = new Grid();

        public Options Options { get; set; } = new Options();

        public List<Output> Outputs { get; set; } = new List<Output>();

        #endregion Public Properties
    }
}