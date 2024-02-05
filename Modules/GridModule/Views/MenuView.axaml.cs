using System.Linq;
using Avalonia.Controls;
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

    public MenuView(MenuItem menuItem)
    {
        this.menuItem = menuItem;
    }

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

    private void OnSubmenuOpened(object sender, Avalonia.Interactivity.RoutedEventArgs e)
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