using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace SportDJ.Modules.MenuModule.Views;

public partial class ItemView
    : UserControl
{
    #region Private Fields

    private readonly MenuItem menuItem;

    #endregion Private Fields

    #region Public Constructors

    public ItemView(MenuItem menuItem)
    {
        this.menuItem = menuItem;
    }

    public ItemView()
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
        var groupItems = this.FindAncestorOfType<ContextMenu>()
            .GetLogicalChildren().OfType<MenuItem>()
            .Where(c => c.DataContext != menuItem.DataContext
                && c.IsSubMenuOpen).ToArray();

        foreach (var groupItem in groupItems)
        {
            groupItem.IsSubMenuOpen = false;
        }
    }

    #endregion Private Methods
}