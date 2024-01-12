namespace SportDJ.Modules.SettingsModule.DesignTime
{
    public class SettingsViewModel
        : ViewModels.SettingsViewModel
    {
        #region Public Constructors

        public SettingsViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default,
                   settingsService: default, sessionService: default)
        { }

        #endregion Public Constructors
    }
}