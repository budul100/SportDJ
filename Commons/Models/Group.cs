using System.Collections.Generic;

namespace SportDJ.Commons.Models
{
    public class Group
        : Content
    {
        #region Public Properties

        public IEnumerable<Content> Contents { get; set; }

        #endregion Public Properties
    }
}