using Avalonia.Controls;
using SportDJ.Modules.GridModule.ViewModels;

namespace SportDJ.Modules.GridModule.Views;

public partial class PlayerView
    : UserControl
{
    #region Public Constructors

    public PlayerView()
    {
        InitializeComponent();

        this.DataContextChanged += OnDataContextChanged;
    }

    #endregion Public Constructors

    #region Private Methods

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        var viewModel = (PlayerViewModel)DataContext;

        if (viewModel != default)
        {
            var contextMenu = this
                .FindControl<ContextMenu>("ContextMenu");

            viewModel.ClosingContextMenuEvent += (s, e) => contextMenu?.Close();
        }
    }

    #endregion Private Methods
}