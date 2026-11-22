namespace SportDJ.Modules.ControllerModule.DesignTime;

public class ControllerViewModel
    : ViewModels.ControllerViewModel
{
    #region Public Constructors

    public ControllerViewModel()
        : base(containerProvider: default, eventAggregator: default,
               settingsService: default, monitoringServices: default)
    { }

    #endregion Public Constructors
}
