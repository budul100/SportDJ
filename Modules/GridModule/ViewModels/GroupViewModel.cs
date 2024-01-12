using Prism.Ioc;
using SharpHook;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class GroupViewModel
        : CellViewModel, IDisposable
    {
        #region Private Fields

        private readonly TaskPoolGlobalHook hook;

        private bool isDisposed;
        private GridViewModel selected;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider, Group group, bool isOnCanvas)
            : base(group)
        {
            Rows = 1;
            Columns = 1;

            hook = containerProvider.Resolve<TaskPoolGlobalHook>();

            if (hook != default)
            {
                hook.KeyPressed += OnKeyPressed;
            }

            InitializeTabs(
                containerProvider: containerProvider,
                group: group,
                isOnCanvas: isOnCanvas);

            this.RowSpan = Rows;
            this.ColumnSpan = Columns;

            RaisePropertyChanged(nameof(Rows));
            RaisePropertyChanged(nameof(Columns));
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns { get; private set; }

        public int Rows { get; private set; }

        public GridViewModel Selected
        {
            get { return selected; }
            set
            {
                if (value != default)
                {
                    SetProperty(ref selected, value);

                    UpdateVisibility();
                }
            }
        }

        public ObservableCollection<GridViewModel> Tabs { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing
                    && hook != default)
                {
                    hook.KeyPressed -= OnKeyPressed;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void InitializeTabs(IContainerProvider containerProvider, Group group, bool isOnCanvas)
        {
            if (group.Grids?.Any() == true)
            {
                foreach (var grid in group.Grids)
                {
                    var gridTab = new GridViewModel(
                        containerProvider: containerProvider,
                        grid: grid,
                        isOnCanvas: false);

                    Tabs.Add(gridTab);
                }

                if (!isOnCanvas)
                {
                    Rows = Tabs.Max(t => t.Rows);
                    Columns = Tabs.Max(t => t.Columns);

                    foreach (var currentTab in Tabs)
                    {
                        currentTab.Rows = Rows;
                        currentTab.Columns = Columns;
                    }
                }
            }
        }

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (IsVisible)
            {
                var selection = Tabs
                    .FirstOrDefault(t => t.Key == e?.Data.KeyCode);

                Selected = selection;
            }
        }

        private void UpdateVisibility()
        {
            foreach (var tab in Tabs)
            {
                if (tab.Cells?.Any() == true)
                {
                    foreach (var cell in tab.Cells)
                    {
                        cell.IsVisible = tab == Selected;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}