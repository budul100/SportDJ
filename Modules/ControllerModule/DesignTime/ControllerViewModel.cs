namespace SportDJ.Modules.ControllerModule.DesignTime
{
    public class ControllerViewModel
        : ViewModels.ControllerViewModel
    {
        #region Public Constructors

        public ControllerViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default, hookService: default,
                   settingsService: default)
        { }

        #endregion Public Constructors
    }
}