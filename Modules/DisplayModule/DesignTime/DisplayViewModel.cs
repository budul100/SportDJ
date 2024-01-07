namespace SportDJ.Modules.DisplayModule.DesignTime
{
    public class DisplayViewModel
        : ViewModels.DisplayViewModel
    {
        #region Public Constructors

        public DisplayViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default)
        {
            Content = "Example - Example";
            Remaining = "00:05.3";
            IsEnding = true;
        }

        #endregion Public Constructors
    }
}