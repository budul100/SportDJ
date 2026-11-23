using System;
using System.Collections.Generic;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Ioc;
using SportDJ.Commons.Models.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public abstract class CellViewModel
    : BindableBase, IDisposable
{
    #region Protected Fields

    protected readonly IContainerProvider containerProvider;
    protected readonly IDialogService dialogService;
    protected readonly IEventAggregator eventAggregator;

    protected IOutputService outputServiceLive;

    #endregion Protected Fields

    #region Private Fields

    private readonly IKeyable keyable;
    private readonly IKeyboardService keyListenService;
    private readonly INameable nameable;
    private readonly IStorageService<Set> settingsService;
    private readonly List<SubscriptionToken> subscriptions = [];

    private Cell cell;
    private bool isDisposed;
    private bool isRenaming;

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
        keyListenService = containerProvider?.Resolve<IKeyboardService>();

        AddSubscription(eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
            action: _ => UpdateOutput(),
            keepSubscriberReferenceAlive: true));
    }

    #endregion Protected Constructors

    #region Public Properties

    public virtual int Column => cell.Column;

    public int ColumnSpan { get; protected set; } = 1;

    public bool HasKey => !string.IsNullOrWhiteSpace(Key);

    public bool IsRenaming
    {
        get { return isRenaming; }
        set { SetProperty(ref isRenaming, value); }
    }

    public string Key => KeyCode?.GetKey();

    public string KeyCode => keyable?.KeyCode;

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

    // While any owner waits for a key to be assigned, others must not react on it
    protected bool IsAnyListening => keyListenService?.IsAny == true;

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

    #region Protected Methods

    protected void AddSubscription(SubscriptionToken subscription)
    {
        if (subscription != default)
        {
            subscriptions.Add(subscription);
        }
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            subscriptions.Clear();

            cell = default;
        }
    }

    protected virtual void Initialize()
    {
        UpdateOutput();
    }

    protected virtual void UpdateOutput()
    {
        outputServiceLive = containerProvider?.Resolve<IOutputService>(
            name: nameof(OutputType.Live));
    }

    #endregion Protected Methods
}
