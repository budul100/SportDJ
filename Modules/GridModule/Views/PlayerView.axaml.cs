using Avalonia.Controls;
using Avalonia.Interactivity;
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

    #region Protected Methods

    protected override void OnDataContextBeginUpdate()
    {
        base.OnDataContextBeginUpdate();

        UpdateContextMenu();
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

    private static void OpenMenu(ContextMenu contextMenu)
    {
        contextMenu?.Open();
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

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        UpdateContextMenu();
    }

    private void UpdateContextMenu()
    {
        var contextMenu = this
            .FindControl<ContextMenu>("ContextMenu");

        if (contextMenu != default)
        {
            var viewModel = DataContext as PlayerViewModel;

            if (viewModel != default)
            {
                viewModel.MenuOpenEvent += (s, e) => OpenMenu(contextMenu);
            }
        }
    }

    #endregion Private Methods
}