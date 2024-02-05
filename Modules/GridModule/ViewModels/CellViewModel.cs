using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        protected readonly IStorageService<Set> settingsService;

        protected bool isDisposed;
        protected IOutputService outputServiceLive;

        #endregion Protected Fields

        #region Private Fields

        private readonly Cell cell;

        private int columnSpan;
        private int rowSpan;

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

            RowSpan = 1;
            ColumnSpan = 1;
        }

        #endregion Protected Constructors

        #region Public Properties

        public int Column => cell.Column;

        public int ColumnSpan
        {
            get { return columnSpan; }
            set { SetProperty(ref columnSpan, value); }
        }

        public bool IsAvailableMenu => !settingsService.Contents.Options.DisableInterruptingDialogs || !outputServiceLive.IsPlaying;

        public ObservableCollection<BindableBase> Menus { get; private set; }

        public int Row => cell.Row;

        public int RowSpan
        {
            get { return rowSpan; }
            set { SetProperty(ref rowSpan, value); }
        }

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

        protected abstract void Dispose(bool isDisposing);

        protected IEnumerable<BindableBase> GetMenuCells()
        {
            var orientations = Enum.GetValues<OrientationType>();

            foreach (var orientation in orientations)
            {
                var header = $"{Constants.GridAddPlayerHeader} {orientation}";

                void callback() => gridService.Add<Player>(
                    cell: cell,
                    orientation: orientation);

                var addPlayerItem = new MenuViewModel(
                    header: header,
                    callback: callback);

                yield return addPlayerItem;
            }

            yield return new SeparatorViewModel();

            foreach (var orientation in orientations)
            {
                var header = $"{Constants.GridMoveHeader} {orientation}";

                void callback() => gridService.Move(
                    cell: cell,
                    orientation: orientation);

                var moveElementItem = new MenuViewModel(
                    header: header,
                    callback: callback);

                yield return moveElementItem;
            }

            yield return new SeparatorViewModel();

            var swapPlayerItem = new MenuViewModel(
                header: $"{Constants.GridSwapHeader}...",
                callback: () => gridService.SwapAsync(cell));

            yield return swapPlayerItem;

            var removeElementItem = new MenuViewModel(
                header: $"{Constants.GridRemoveHeader}...",
                callback: () => gridService.RemoveAsync(cell));

            yield return removeElementItem;
        }

        protected abstract IEnumerable<BindableBase> GetMenuItems();

        protected virtual void Intialize()
        {
            UpdateOutput();
            UpdateMenu();
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