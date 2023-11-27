using SportDJ.Commons.Models.Settings;

namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class PlayerViewModel
        : ViewModels.PlayerViewModel
    {
        #region Public Constructors

        public PlayerViewModel()
            : this("Player", 0, 0)
        { }

        public PlayerViewModel(string name, int row, int column)
        {
            Initialize(new Player { Name = name, Row = row, Column = column });
        }

        #endregion Public Constructors
    }
}