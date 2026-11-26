using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SportDJ.Modules.DefinitionsModule.Views;

public partial class ControllerSettingsView
    : UserControl
{
    #region Public Constructors

    public ControllerSettingsView()
    {
        InitializeComponent();

        // Takes over the focus from text inputs, so that key presses reach the hotkeys again
        Focusable = true;
    }

    #endregion Public Constructors

    #region Private Methods

    private void OnListenClick(object sender, RoutedEventArgs e)
    {
        Focus();
    }

    #endregion Private Methods
}
