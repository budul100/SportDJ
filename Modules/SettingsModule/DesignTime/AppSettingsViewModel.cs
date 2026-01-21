namespace SportDJ.Modules.SettingsModule.DesignTime
{
    public class AppSettingsViewModel
        : ViewModels.AppSettingsViewModel
    {
        #region Public Constructors

        public AppSettingsViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default, dialogService: default,
                   panelService: default, settingsService: default, sessionService: default, controllerService: default)
        { }

        #endregion Public Constructors
    }
}