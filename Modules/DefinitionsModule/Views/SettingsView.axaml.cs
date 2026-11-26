using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SportDJ.Modules.DefinitionsModule.ViewModels;

namespace SportDJ.Modules.DefinitionsModule.Views;

public partial class SettingsView
    : UserControl
{
    #region Public Constructors

    public SettingsView()
    {
        InitializeComponent();

        // Takes over the focus from text inputs, so that key presses reach the hotkeys again
        Focusable = true;
    }

    #endregion Public Constructors

    #region Private Methods

    private void ClearFocus()
    {
        Focus();
    }

    private void OnListenClick(object sender, RoutedEventArgs e)
    {
        ClearFocus();
    }

    private void OnNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter
            || e.Key == Key.Escape)
        {
            ClearFocus();

            e.Handled = true;
        }
    }

    // An empty name is not stored, so the stored name is shown again as soon as the input is left
    private void OnNameLostFocus(object sender, RoutedEventArgs e)
    {
        (DataContext as SettingsViewModel)?.RestoreName();
    }

    #endregion Private Methods
}
