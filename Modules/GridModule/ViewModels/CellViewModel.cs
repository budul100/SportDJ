using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using SportDJ.Modules.GridModule.DesignTime;

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

        protected bool isListening;
        protected Action listenChecker;
        protected IOutputService outputServiceLive;

        #endregion Protected Fields

        #region Private Fields

        private readonly IHookService hookService;
        private readonly IKeyable keyable;
        private readonly INameable nameable;
        private readonly IStorageService<Set> settingsService;

        private Cell cell;
        private bool isDisposed;
        private bool isRenaming;
        private string name;

        #endregion Private Fields

        #region Protected Constructors

        protected CellViewModel(IContainerProvider containerProvider, object element)
        {
            this.containerProvider = containerProvider;

            this.cell = element is Grid grid
                ? grid?.Group
                : element as Cell;

            nameable = element as INameable;
            keyable = element as IKeyable;

            eventAggregator = containerProvider?.Resolve<IEventAggregator>();
            settingsService = containerProvider?.Resolve<IStorageService<Set>>();
            dialogService = containerProvider?.Resolve<IDialogService>();
            gridService = containerProvider?.Resolve<IGridService>();
            hookService = containerProvider?.Resolve<IHookService>();

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateOutput(),
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<HookUpdatedEvent>().Subscribe(
                action: UpdateKey,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Protected Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        public event EventHandler MenuOpenEvent;

        #endregion Public Events

        #region Public Properties

        public static int MaxLengthName => Constants.NameMaxLength;

        public int Column => cell.Column;

        public int ColumnSpan { get; protected set; } = 1;

        public bool HasKey => !string.IsNullOrWhiteSpace(Key);

        public virtual bool IsAvailableHook => hookService?.IsRunning == true;

        public bool IsAvailableMenu => outputServiceLive?.IsPlaying == false
                    || settingsService?.Contents?.Options?.EnableInterruptingDialogs == true;

        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                if (IsRenaming != value)
                {
                    SetProperty(ref isRenaming, value);

                    if (value)
                    {
                        hookService.Deactivate(this);
                    }
                    else
                    {
                        hookService.Activate(this);
                    }
                }
            }
        }

        public string Key => KeyCode?.GetKey();

        public string KeyCode
        {
            get
            {
                return hookService?.IsAvailable == true
                    ? keyable?.KeyCode
                    : default;
            }

            set
            {
                if (hookService?.IsAvailable == true
                    && keyable != default
                    && value != keyable.KeyCode)
                {
                    keyable.KeyCode = value;

                    settingsService?.Save();

                    RaisePropertyChanged(nameof(KeyCode));
                    RaisePropertyChanged(nameof(HasKey));
                    RaisePropertyChanged(nameof(Key));
                }
            }
        }

        public ObservableCollection<BindableBase> Menus { get; private set; }

        public string Name
        {
            get { return name ?? nameable?.Name; }

            set
            {
                if (nameable != default
                    && value != name)
                {
                    name = value;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        nameable.Name = value.Trim();

                        settingsService?.Save();
                    }

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

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

        protected void CloseMenu()
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);

            RaisePropertyChanged(nameof(Menus));
        }

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

        protected virtual IEnumerable<BindableBase> GetElementMenus()
        {
            yield break;
        }

        protected virtual IEnumerable<BindableBase> GetGridMenus()
        {
            var orientations = Enum.GetValues<OrientationType>();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.GridAddHeader} {orientation}";

                void callback() => gridService.AddPlayer(
                    cell: cell,
                    orientation: orientation);

                var addItem = new MenuViewModel(
                    header: header,
                    executeCallback: callback,
                    closeCallback: CloseMenu);

                yield return addItem;
            }

            yield return new SeparatorViewModel();

            foreach (var orientation in orientations)
            {
                var header = $"{Texts.GridMoveHeader} {orientation}";

                void callback() => gridService.MoveCell(
                    cell: cell,
                    orientation: orientation);

                var moveItem = new MenuViewModel(
                    header: header,
                    executeCallback: callback,
                    closeCallback: CloseMenu);

                yield return moveItem;
            }

            yield return new SeparatorViewModel();

            var swapItem = new MenuViewModel(
                header: $"{Texts.CellSwapHeader}...",
                executeCallback: () => gridService.SwapAsync(cell),
                closeCallback: CloseMenu);

            yield return swapItem;

            var removeItem = new MenuViewModel(
                header: $"{Texts.CellRemoveHeader}...",
                executeCallback: () => gridService.RemoveCellAsync(cell),
                closeCallback: CloseMenu);

            yield return removeItem;
        }

        protected virtual void Initialize()
        {
            UpdateOutput();

            var menus = GetElementMenus();
            Menus = new ObservableCollection<BindableBase>(menus);

            RaisePropertyChanged(nameof(Menus));
        }

        protected void ListenHook()
        {
            if (hookService != default
                && !isListening)
            {
                isListening = true;

                hookService.Listen(ReceiveHook);
            }
        }

        protected void OpenMenu()
        {
            MenuOpenEvent?.Invoke(
                sender: this,
                e: default);
        }

        protected virtual void UpdateOutput()
        {
            outputServiceLive = containerProvider?.Resolve<IOutputService>(
                name: nameof(OutputType.Live));
        }

        #endregion Protected Methods

        #region Private Methods

        private void ReceiveHook(string keyCode)
        {
            if (isListening)
            {
                KeyCode = keyCode;
                isListening = false;

                listenChecker.Invoke();

                CloseMenu();
            }
        }

        private void UpdateKey()
        {
            RaisePropertyChanged(nameof(KeyCode));
            RaisePropertyChanged(nameof(Key));
            RaisePropertyChanged(nameof(HasKey));
            RaisePropertyChanged(nameof(IsAvailableHook));
        }

        #endregion Private Methods
    }
}