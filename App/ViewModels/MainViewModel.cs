using Prism.Mvvm;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Public Constructors

        public MainViewModel(ISettingsService<Set> settingsService)
        {
        }

        #endregion Public Constructors
    }
}