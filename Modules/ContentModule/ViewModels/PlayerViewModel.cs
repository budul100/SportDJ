using Prism.Mvvm;
using SportDJ.Commons.Models;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class PlayerViewModel
        : BindableBase
    {
        #region Public Properties

        public string Name { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Player player)
        {
            Name = player.Name;
            this.RaisePropertyChanged(nameof(Name));
        }

        #endregion Public Methods
    }
}