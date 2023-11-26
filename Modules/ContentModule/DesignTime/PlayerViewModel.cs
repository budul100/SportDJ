namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class PlayerViewModel
        : ViewModels.BaseViewModel
    {
        #region Public Constructors

        public PlayerViewModel()
            : this("Player")
        { }

        public PlayerViewModel(string name)
        {
            Initialize(
                name: name);
        }

        #endregion Public Constructors
    }
}