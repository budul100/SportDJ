using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Models.Contents
{
    public class Controller
        : Cell
    {
        #region Public Properties

        public string Path { get; set; }

        public PlayerType PlayerType { get; set; }

        public bool SavePositions { get; set; }

        #endregion Public Properties
    }
}