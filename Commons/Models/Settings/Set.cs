using SportDJ.Commons.Models.Contents;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public class Set
    {
        #region Public Properties

        public Options Options { get; set; } = new Options();

        public List<Output> Outputs { get; set; } = new List<Output>();

        public Root Root { get; set; } = new Root();

        #endregion Public Properties
    }
}