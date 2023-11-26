using Prism.Mvvm;
using SportDJ.Commons.Interfaces;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Public Constructors

        public MainViewModel(IContentService contentService)
        {
            contentService.Load();
        }

        #endregion Public Constructors
    }
}