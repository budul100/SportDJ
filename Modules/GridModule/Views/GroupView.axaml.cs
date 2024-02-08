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

    #region Protected Methods

    protected override void OnDataContextBeginUpdate()
    {
        base.OnDataContextBeginUpdate();
    }

    #endregion Protected Methods

    #region Private Methods

    private static void CloseMenu(ContextMenu contextMenu)
    {
        if (contextMenu?.IsOpen == true)
        {
            contextMenu.Close();
        }
    }

    private void OnContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            var viewModel = contextMenu.DataContext as CellViewModel;

            if (viewModel != default)
            {
                viewModel.MenuCloseEvent += (s, e) => CloseMenu(contextMenu);
            }
        }
    }

    #endregion Private Methods
}