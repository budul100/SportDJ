using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SportDJ.Modules.GridModule.ViewModels;

namespace SportDJ.Modules.GridModule.Views;

public partial class GroupView
    : UserControl
{
    #region Private Fields

    private ContextMenu openedContextMenu;
    private CellViewModel openedViewModel;

    #endregion Private Fields

    #region Public Constructors

    public GroupView()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Private Methods

    private void Detach()
    {
        if (openedViewModel != default)
        {
            openedViewModel.MenuCloseEvent -= OnMenuClose;
        }

        openedViewModel = default;
        openedContextMenu = default;
    }

    private void OnContextMenuClosed(object sender, RoutedEventArgs e)
    {
        Detach();
    }

    private void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        // The handler is only attached while the menu is open, otherwise it would pile up on each opening
        Detach();

        if (sender is ContextMenu contextMenu
            && contextMenu.DataContext is CellViewModel viewModel)
        {
            openedContextMenu = contextMenu;
            openedViewModel = viewModel;

            viewModel.MenuCloseEvent += OnMenuClose;
        }
    }

    private void OnMenuClose(object sender, EventArgs e)
    {
        var contextMenu = openedContextMenu;

        if (contextMenu != default)
        {
            // The event might be raised from a non UI thread (e.g. by a key press)
            Dispatcher.UIThread.Post(contextMenu.Close);
        }
    }

    #endregion Private Methods
}
