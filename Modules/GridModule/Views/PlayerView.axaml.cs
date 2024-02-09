using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

    private void OnButtonKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            var viewModel = contextMenu.DataContext as CellViewModel;

            if (viewModel != default)
            {
                viewModel.MenuCloseEvent += (s, e) => Dispatcher.UIThread.Invoke(() => contextMenu.Close());
            }
        }
    }

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        var contextMenu = this
            .FindControl<ContextMenu>("ContextMenu");

        if (contextMenu != default)
        {
            var viewModel = DataContext as PlayerViewModel;

            if (viewModel != default)
            {
                viewModel.MenuOpenEvent += (s, e) => contextMenu.Open();
            }
        }
    }

    #endregion Private Methods
}