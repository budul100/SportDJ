using SportDJ.Commons.Models.Settings;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class ControllerViewModel
        : CellViewModel
    {
        #region Public Methods

        public void Initialize(Controller player)
        {
            base.Initialize(
                cell: player);
        }

        #endregion Public Methods
    }
}