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
        var contextMenu = this
            .FindControl<ContextMenu>("ContextMenu");

        if (contextMenu != default)
        {
            var viewModel = DataContext as PlayerViewModel;

            if (viewModel != default)
            {
                viewModel.MenuCloseEvent += (s, e) => { if (contextMenu.ItemCount > 0) contextMenu.Close(); };
                viewModel.MenuOpenEvent += (s, e) => { if (contextMenu.ItemCount > 0) contextMenu.Open(); };
            }
        }
    }

    #endregion Private Methods
}