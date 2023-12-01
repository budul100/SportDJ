using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties
        public List<Output> Outputs { get; set; } = new List<Output>();

        public Options Options { get; set; } = new Options();

        public Set Set { get; set; } = new Set();

        #endregion Public Properties
    }
}