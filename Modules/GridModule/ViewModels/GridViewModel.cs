using Material.Icons;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels;

public class GridViewModel
    : CellViewModel
{
    #region Private Fields

    private readonly Grid grid;
    private readonly GroupViewModel parent;

    private int columns;
    private bool isSelected;
    private int rows;

    #endregion Private Fields

    #region Public Constructors

    public GridViewModel(IContainerProvider containerProvider, Grid grid, GroupViewModel parent = default)
        : base(containerProvider: containerProvider, element: grid)
    {
        this.grid = grid;
        this.parent = parent;

        InitializeCells(
            containerProvider: containerProvider,
            grid: grid);
    }

    #endregion Public Constructors

    #region Public Properties

    public ObservableCollection<CellViewModel> Cells { get; } = [];

    public int Columns
    {
        get { return columns; }
        set { SetProperty(ref columns, value); }
    }

    public bool IsSelected
    {
        get
        {
            return isSelected || parent == default;
        }
        set
        {
            isSelected = value;
        }
    }

    public bool IsVisible => IsSelected
        && parent?.IsVisible != false;

    public int Rows
    {
        get { return rows; }
        set { SetProperty(ref rows, value); }
    }

    #endregion Public Properties

    #region Protected Methods

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var child in Cells.ToArray())
            {
                child.Dispose();
            }
        }

        base.Dispose(
            isDisposing: isDisposing);
    }

    protected override IEnumerable<BindableBase> GetElementMenus()
    {
        var gridMenus = base.GetGridMenus().ToArray();

        yield return new MenuViewModel(
            header: $"{Texts.GridEditHeader}...",
            menuItems: gridMenus,
            closeCallback: CloseMenu);

        var groupMenus = GetGroupMenus().ToArray();

        yield return new MenuViewModel(
            header: $"{Texts.GroupEditHeader}...",
            menuItems: groupMenus,
            closeCallback: CloseMenu);

        var tabMenus = GetTabMenus().ToArray();

        yield return new MenuViewModel(
            header: $"{Texts.TabEditHeader}...",
            menuItems: tabMenus,
            closeCallback: CloseMenu);
    }

    #endregion Protected Methods

    #region Private Methods

    private IEnumerable<BindableBase> GetGroupMenus()
    {
        var orientations = Enum.GetValues<OrientationType>()
            .Where(o => o == OrientationType.Left || o == OrientationType.Right).ToArray();

        foreach (var orientation in orientations)
        {
            yield return new MenuViewModel(
                header: $"{Texts.GroupAddHeader} {orientation}",
                executeCallback: () => gridService.AddGrid(
                    grid: grid,
                    orientation: orientation),
                closeCallback: CloseMenu);
        }

        yield return new SeparatorViewModel();

        foreach (var orientation in orientations)
        {
            yield return new MenuViewModel(
                header: $"{Texts.GroupMoveHeader} {orientation}",
                executeCallback: () => gridService.MoveGrid(
                    grid: grid,
                    orientation: orientation),
                closeCallback: CloseMenu);
        }

        yield return new SeparatorViewModel();

        yield return new MenuViewModel(
            header: $"{Texts.TabRemoveHeader}...",
            executeCallback: () => gridService.RemoveGridAsync(grid),
            closeCallback: CloseMenu);
    }

    private IEnumerable<BindableBase> GetTabMenus()
    {
        yield return new MenuViewModel(
            header: Texts.TabRenameHeader,
            executeCallback: () => IsRenaming = true,
            closeCallback: CloseMenu,
            icon: MaterialIconKind.FormTextbox);

        var listenItem = new MenuViewModel(
            header: Texts.KeyListenHeader,
            executeCallback: ListenHook,
            isCheckedGetter: () => IsListening,
            icon: MaterialIconKind.KeyboardOutline);

        listenChecker = listenItem.Update;

        yield return listenItem;

        yield return new MenuViewModel(
            header: Texts.KeyUnhookHeader,
            executeCallback: () => KeyCode = default,
            closeCallback: CloseMenu,
            icon: MaterialIconKind.KeyboardOffOutline);
    }

    private void InitializeCells(IContainerProvider containerProvider, Grid grid)
    {
        var rowsCount = 0;
        var columnsCount = 0;

        if (grid?.Cells?.Count > 0)
        {
            var isOnCanvas = parent == default
                && grid.Cells.Count == 1;

            foreach (var element in grid.Cells)
            {
                switch (element)
                {
                    case Player player:
                        {
                            var playerCell = new PlayerViewModel(
                                containerProvider: containerProvider,
                                player: player,
                                parent: this);

                            Cells.Add(playerCell);

                            rowsCount = Math.Max(rowsCount, element.Row + 1);
                            columnsCount = Math.Max(columnsCount, element.Column + 1);

                            break;
                        }

                    case Group group:
                        {
                            var groupCell = new GroupViewModel(
                                containerProvider: containerProvider,
                                group: group,
                                parent: this,
                                isOnCanvas: isOnCanvas);

                            Cells.Add(groupCell);

                            rowsCount = Math.Max(rowsCount, groupCell.Row + groupCell.Rows);
                            columnsCount = Math.Max(columnsCount, groupCell.Column + groupCell.Columns);

                            break;
                        }
                }
            }
        }

        Rows = rowsCount;
        Columns = columnsCount;

        // Also an empty grid needs its menus, otherwise no first element could be added
        base.Initialize();
    }

    #endregion Private Methods
}