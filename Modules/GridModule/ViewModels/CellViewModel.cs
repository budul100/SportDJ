using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels;

public abstract class CellViewModel
    : BindableBase, IDisposable
{
    #region Protected Fields

    protected readonly IContainerProvider containerProvider;
    protected readonly IDialogService dialogService;
    protected readonly IEventAggregator eventAggregator;
    protected readonly IGridService gridService;

    protected Action listenChecker;
    protected IOutputService outputServiceLive;

    #endregion Protected Fields

    #region Private Fields

    // All cells which currently wait for a key to be assigned. Shared, so that
    // other cells do not react on the key which is meant to be assigned.
    private static readonly HashSet<CellViewModel> listeningCells = [];
    private static readonly Lock listeningLock = new();

    private Cell cell;
    private bool isDisposed;
    private bool isRenaming;

    private readonly IKeyable keyable;
    private readonly INameable nameable;
    private readonly IStorageService<Set> settingsService;
    private readonly List<SubscriptionToken> subscriptions = [];

    #endregion Private Fields

    #region Protected Constructors

    protected CellViewModel(IContainerProvider containerProvider, object element)
    {
        this.containerProvider = containerProvider;

        cell = element is Grid grid
            ? grid.Group
            : element as Cell;

        nameable = element as INameable;
        keyable = element as IKeyable;

        eventAggregator = containerProvider?.Resolve<IEventAggregator>();
        settingsService = containerProvider?.Resolve<IStorageService<Set>>();
        dialogService = containerProvider?.Resolve<IDialogService>();
        gridService = containerProvider?.Resolve<IGridService>();

        AddSubscription(eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
            action: _ => UpdateOutput(),
            keepSubscriberReferenceAlive: true));
    }

    #endregion Protected Constructors

    #region Public Events

    public event EventHandler MenuCloseEvent;

    public event EventHandler MenuOpenEvent;

    #endregion Public Events

    #region Public Properties

    public virtual int Column => cell.Column;

    public int ColumnSpan { get; protected set; } = 1;

    public bool HasKey => !string.IsNullOrWhiteSpace(Key);

    public bool IsListening
    {
        get
        {
            lock (listeningLock)
            {
                return listeningCells.Contains(this);
            }
        }
    }

    public bool IsRenaming
    {
        get { return isRenaming; }
        set { SetProperty(ref isRenaming, value); }
    }

    public string Key => KeyCode?.GetKey();

    public string KeyCode
    {
        get
        {
            return keyable?.KeyCode;
        }

        set
        {
            if (keyable != default
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

    public virtual string Name
    {
        get { return nameable?.Name; }

        set
        {
            if (nameable != default)
            {
                var trimmed = value?.Trim();

                if (!string.IsNullOrWhiteSpace(trimmed)
                    && trimmed != nameable.Name)
                {
                    nameable.Name = trimmed;

                    settingsService?.Save();
                }

                // Always notify, so the UI falls back to the stored name after an invalid input
                RaisePropertyChanged(nameof(Name));
            }
        }
    }

    public virtual int Row => cell.Row;

    public int RowSpan { get; protected set; } = 1;

    #endregion Public Properties

    #region Protected Properties

    protected static bool IsAnyListening
    {
        get
        {
            lock (listeningLock)
            {
                return listeningCells.Count > 0;
            }
        }
    }

    #endregion Protected Properties

    #region Public Methods

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }
    }

    #endregion Public Methods

    #region Protected Internal Methods

    protected internal void ReceiveKey(string keyCode)
    {
        lock (listeningLock)
        {
            listeningCells.Remove(this);
        }

        KeyCode = keyCode;

        listenChecker?.Invoke();

        CloseMenu();
    }

    #endregion Protected Internal Methods

    #region Protected Methods

    protected void AddSubscription(SubscriptionToken subscription)
    {
        if (subscription != default)
        {
            subscriptions.Add(subscription);
        }
    }

    protected void CloseMenu()
    {
        MenuCloseEvent?.Invoke(
            sender: this,
            e: default);

        RaisePropertyChanged(nameof(Menus));
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            lock (listeningLock)
            {
                listeningCells.Remove(this);
            }

            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            subscriptions.Clear();

            DisposeMenus();

            cell = default;
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
            yield return new MenuViewModel(
                header: $"{Texts.GridAddHeader} {orientation}",
                executeCallback: () => gridService.AddPlayer(
                    cell: cell,
                    orientation: orientation),
                closeCallback: CloseMenu);
        }

        yield return new SeparatorViewModel();

        foreach (var orientation in orientations)
        {
            yield return new MenuViewModel(
                header: $"{Texts.GridMoveHeader} {orientation}",
                executeCallback: () => gridService.MoveCell(
                    cell: cell,
                    orientation: orientation),
                closeCallback: CloseMenu);
        }

        yield return new SeparatorViewModel();

        yield return new MenuViewModel(
            header: $"{Texts.CellSwapHeader}...",
            executeCallback: () => gridService.SwapAsync(cell),
            closeCallback: CloseMenu);

        yield return new MenuViewModel(
            header: $"{Texts.CellRemoveHeader}...",
            executeCallback: () => gridService.RemoveCellAsync(cell),
            closeCallback: CloseMenu);
    }

    protected virtual void Initialize()
    {
        UpdateOutput();

        // The old menu items hold event subscriptions, which have to be released
        DisposeMenus();

        Menus = new ObservableCollection<BindableBase>(GetElementMenus());

        RaisePropertyChanged(nameof(Menus));
    }

    protected void ListenHook()
    {
        lock (listeningLock)
        {
            listeningCells.Add(this);
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

    private void DisposeMenus()
    {
        if (Menus != default)
        {
            foreach (var menu in Menus.OfType<IDisposable>().ToArray())
            {
                menu.Dispose();
            }
        }
    }

    #endregion Private Methods
}
