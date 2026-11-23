using Avalonia.Controls;

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
}
