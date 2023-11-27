using Prism.Mvvm;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public abstract class BaseViewModel
        : BindableBase
    {
        #region Public Properties

        public string Name { get; private set; }

        #endregion Public Properties

        #region Protected Methods

        protected void Initialize(string name)
        {
            Name = name;
        }

        #endregion Protected Methods
    }
}