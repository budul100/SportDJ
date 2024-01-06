using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Structure
{
    public class Player
        : Element
    {
        #region Public Properties

        public string Path { get; set; }

        public bool SavePositions { get; set; }

        public PlayerType Type { get; set; }

        #endregion Public Properties
    }
}