using SportDJ.Commons.Models.Structure;

namespace SportDJ.Commons.Models.Settings
{
    public class Set
    {
        #region Public Properties

        public Grid Grid { get; set; } = new Grid();

        public Options Options { get; set; } = new Options();

        #endregion Public Properties
    }
}