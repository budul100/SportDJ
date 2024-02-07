using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public abstract class CellViewModel
        : BindableBase, IDisposable
    {
        #region Protected Fields

        protected readonly IContainerProvider containerProvider;
        protected readonly IDialogService dialogService;
        protected readonly IEventAggregator eventAggregator;
        protected readonly IGridService gridService;

        protected IOutputService outputServiceLive;

        #endregion Protected Fields

        #region Private Fields

        private readonly IStorageService<Set> settingsService;

        private Cell cell;
        private bool isDisposed;

        #endregion Private Fields

        #region Protected Constructors

        protected CellViewModel(IContainerProvider containerProvider, Cell cell)
        {
            this.containerProvider = containerProvider;
            this.cell = cell;

            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            settingsService = containerProvider.Resolve<IStorageService<Set>>();
            dialogService = containerProvider.Resolve<IDialogService>();
            gridService = containerProvider.Resolve<IGridService>();

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateOutput(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Protected Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        #endregion Public Events

        #region Public Properties

        public int Column => cell.Column;

        public int ColumnSpan { get; protected set; } = 1;

        public bool IsAvailableMenu => outputServiceLive?.IsPlaying == false
            || !settingsService.Contents.Options.DisableInterruptingDialogs;

        public ObservableCollection<BindableBase> Menus { get; private set; }

        public int Row => cell.Row;

        public int RowSpan { get; protected set; } = 1;

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
                if (isDisposing)
                {
                    cell = default;
                }

                isDisposed = true;
            }
        }

        protected IEnumerable<BindableBase> GetMenuCells()
        {
            var orientations = Enum.GetValues<OrientationType>();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.GridAddPlayerHeader} {orientation}";

                void callback() => gridService.Add<Player>(
                    cell: cell,
                    orientation: orientation);

                var addPlayerItem = new MenuViewModel(
                    header: header,
                    callback: callback);

                addPlayerItem.MenuCloseEvent += OnMenuClosing;

                yield return addPlayerItem;
            }

            yield return new SeparatorViewModel();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.GridMoveHeader} {orientation}";

                void callback() => gridService.Move(
                    cell: cell,
                    orientation: orientation);

                var moveElementItem = new MenuViewModel(
                    header: header,
                    callback: callback);

                moveElementItem.MenuCloseEvent += OnMenuClosing;

                yield return moveElementItem;
            }

            yield return new SeparatorViewModel();

            var swapPlayerItem = new MenuViewModel(
                header: $"{Texts.GridSwapHeader}...",
                callback: () => gridService.SwapAsync(cell));

            swapPlayerItem.MenuCloseEvent += OnMenuClosing;

            yield return swapPlayerItem;

            var removeElementItem = new MenuViewModel(
                header: $"{Texts.GridRemoveHeader}...",
                callback: () => gridService.RemoveAsync(cell));

            removeElementItem.MenuCloseEvent += OnMenuClosing;

            yield return removeElementItem;
        }

        protected abstract IEnumerable<BindableBase> GetMenuItems();

        protected virtual void Initialize()
        {
            UpdateOutput();
            UpdateMenu();
        }

        protected void OnMenuClosing(object sender, EventArgs e)
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);
        }

        protected void UpdateMenu()
        {
            var menus = GetMenuItems();

            Menus = new ObservableCollection<BindableBase>(menus);
        }

        protected virtual void UpdateOutput()
        {
            outputServiceLive = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));
        }

        #endregion Protected Methods
    }
}