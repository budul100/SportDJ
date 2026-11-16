namespace SportDJ.Modules.SettingsModule.DesignTime;

public class SettingsViewModel
    : ViewModels.SettingsViewModel
{
    #region Public Constructors

    public SettingsViewModel()
        : base(containerProvider: default, eventAggregator: default, dialogService: default, panelService: default,
               settingsService: default, sessionService: default, controllerService: default)
    { }

    #endregion Public Constructors
}