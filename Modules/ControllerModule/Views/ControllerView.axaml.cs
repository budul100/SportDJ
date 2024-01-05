using Avalonia.Controls;
using Avalonia.Interactivity;
using SportDJ.Modules.ControllerModule.ViewModels;

namespace SportDJ.Modules.ControllerModule.Views;

public partial class ControllerView
    : UserControl
{
    #region Private Fields

    private readonly ControllerViewModel viewModel;

    #endregion Private Fields

    #region Public Constructors

    public ControllerView()
    {
        InitializeComponent();

        viewModel = (ControllerViewModel)DataContext;

        var volumeButton = this
            .FindControl<Button>("VolumeButton");

        volumeButton.AddHandler(
            Button.PointerPressedEvent,
            (_, __) => OnVolumeLower(),
            routes: RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);

        volumeButton.AddHandler(
            Button.PointerReleasedEvent,
            (_, __) => OnVolumeRaise(),
            routes: RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    #endregion Public Constructors

    #region Private Methods

    private void OnVolumeLower()
    {
        if (viewModel.VolumeLowerCommand.CanExecute())
        {
            viewModel.VolumeLowerCommand.Execute();
        }
    }

    private void OnVolumeRaise()
    {
        if (viewModel.VolumeRaiseCommand.CanExecute())
        {
            viewModel.VolumeRaiseCommand.Execute();
        }
    }

    #endregion Private Methods
}