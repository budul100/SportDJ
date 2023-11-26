using SportDJ.Commons.Models;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class PlayerViewModel
        : BaseViewModel
    {
        #region Public Methods

        public void Initialize(Player player)
        {
            Initialize(player.Name);
        }

        #endregion Public Methods
    }
}