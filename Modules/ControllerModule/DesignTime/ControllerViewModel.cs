namespace SportDJ.Modules.ControllerModule.DesignTime
{
    public class ControllerViewModel
        : ViewModels.ControllerViewModel
    {
        #region Public Constructors

        public ControllerViewModel()
            : base(containerProvider: default, eventAggregator: default)
        {
            Content = "Example - Example";
            Remaining = "00:05.3";
        }

        #endregion Public Constructors
    }
}