using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SportDJ.Modules.DefinitionsModule.Views;

public partial class LayoutView
    : UserControl
{
    #region Public Constructors

    public LayoutView()
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

    private void OnNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter
            || e.Key == Key.Escape)
        {
            Focus();

            e.Handled = true;
        }
    }

    #endregion Private Methods
}
