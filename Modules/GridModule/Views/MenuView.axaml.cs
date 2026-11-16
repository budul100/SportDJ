using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace SportDJ.Modules.GridModule.Views;

public partial class MenuView
    : UserControl
{
    #region Private Fields

    private readonly MenuItem menuItem;

    #endregion Private Fields

    #region Public Constructors

    public MenuView()
    {
        InitializeComponent();

        menuItem = this
            .FindControl<MenuItem>("MenuItem");

        if (menuItem != default)
        {
            menuItem.SubmenuOpened += OnSubmenuOpened;
        }
    }

    #endregion Public Constructors

    #region Private Methods

    private void OnSubmenuOpened(object sender, RoutedEventArgs e)
    {
        var contextMenu = this.FindAncestorOfType<ContextMenu>();

        if (contextMenu != default)
        {
            var groupItems = contextMenu
                .GetLogicalChildren().OfType<MenuItem>()
                .Where(c => c.DataContext != menuItem.DataContext
                    && c.IsSubMenuOpen).ToArray();

            foreach (var groupItem in groupItems)
            {
                groupItem.IsSubMenuOpen = false;
            }
        }
    }

    #endregion Private Methods
}
