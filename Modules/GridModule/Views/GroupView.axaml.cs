using Avalonia.Controls;
using Avalonia.Interactivity;
using SportDJ.Modules.GridModule.ViewModels;

namespace SportDJ.Modules.GridModule.Views;

public partial class GroupView
    : UserControl
{
    #region Public Constructors

    public GroupView()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Private Methods

    private void OnContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            var viewModel = contextMenu.DataContext as CellViewModel;

            if (viewModel != default)
            {
                viewModel.MenuCloseEvent += (s, e) => contextMenu.Close();
            }
        }
    }

    #endregion Private Methods
}