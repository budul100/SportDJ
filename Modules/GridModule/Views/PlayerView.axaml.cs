using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SportDJ.Modules.GridModule.ViewModels;

namespace SportDJ.Modules.GridModule.Views;

public partial class PlayerView
    : UserControl
{
    #region Private Fields

    private Control button;
    private ContextMenu contextMenu;
    private PlayerViewModel currentViewModel;

    #endregion Private Fields

    #region Public Constructors

    public PlayerView()
    {
        InitializeComponent();

        this.DataContextChanged += OnDataContextChanged;
    }

    #endregion Public Constructors

    #region Private Methods

    private Control FindControl(Control parent, ContextMenu targetContextMenu)
    {
        if (parent is Button button
            && button.ContextMenu == targetContextMenu)
        {
            return button;
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control control)
                {
                    var result = FindControl(
                        parent: control,
                        targetContextMenu: targetContextMenu);

                    if (result != default)
                    {
                        return result;
                    }
                }
            }
        }
        else if (parent is ContentControl contentControl
            && contentControl.Content is Control childControl)
        {
            return FindControl(
                parent: childControl,
                targetContextMenu: targetContextMenu);
        }
        else if (parent is Decorator decorator
            && decorator.Child is Control decoratorChild)
        {
            return FindControl(
                parent: decoratorChild,
                targetContextMenu: targetContextMenu);
        }

        return default;
    }

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
        if (currentViewModel != default)
        {
            currentViewModel.MenuOpenEvent -= OnMenuOpen;
        }

        contextMenu = this.FindControl<ContextMenu>("ContextMenu");

        if (contextMenu != default)
        {
            var viewModel = DataContext as PlayerViewModel;

            if (viewModel != default)
            {
                currentViewModel = viewModel;

                button = FindControl(
                    parent: this,
                    targetContextMenu: contextMenu);

                if (button != default)
                {
                    viewModel.MenuOpenEvent += OnMenuOpen;
                }
            }
        }
    }

    private void OnMenuOpen(object s, System.EventArgs args)
    {
        contextMenu.Open(button);
    }

    #endregion Private Methods
}