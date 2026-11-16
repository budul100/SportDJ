using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SportDJ.Modules.GridModule.ViewModels;

namespace SportDJ.Modules.GridModule.Views;

public partial class PlayerView
    : UserControl
{
    #region Private Fields

    private readonly Button executeButton;

    private bool isAttached;
    private ContextMenu openedContextMenu;
    private CellViewModel openedViewModel;
    private PlayerViewModel subscribedViewModel;

    #endregion Private Fields

    #region Public Constructors

    public PlayerView()
    {
        InitializeComponent();

        executeButton = this.FindControl<Button>("ExecuteButton");

        DataContextChanged += OnDataContextChanged;
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        isAttached = true;

        UpdateSubscription();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        isAttached = false;

        UpdateSubscription();

        base.OnDetachedFromVisualTree(e);
    }

    #endregion Protected Methods

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

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        UpdateSubscription();
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

    private void OnMenuOpen(object sender, EventArgs e)
    {
        // The event might be raised from a non UI thread (e.g. by a key press)
        Dispatcher.UIThread.Post(() => executeButton?.ContextMenu?.Open(executeButton));
    }

    private void UpdateSubscription()
    {
        // Only views in the visual tree listen, otherwise replaced views would stay subscribed
        var viewModel = isAttached
            ? DataContext as PlayerViewModel
            : default;

        if (viewModel != subscribedViewModel)
        {
            if (subscribedViewModel != default)
            {
                subscribedViewModel.MenuOpenEvent -= OnMenuOpen;
            }

            subscribedViewModel = viewModel;

            if (subscribedViewModel != default)
            {
                subscribedViewModel.MenuOpenEvent += OnMenuOpen;
            }
        }
    }

    #endregion Private Methods
}
