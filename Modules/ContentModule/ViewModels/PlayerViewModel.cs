using SportDJ.Commons.Models.Settings;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel
    {
        #region Public Methods

        public void Initialize(Player player)
        {
            base.Initialize(
                cell: player);
        }

        #endregion Public Methods
    }
}