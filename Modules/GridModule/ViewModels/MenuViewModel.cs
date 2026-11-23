using System;
using System.Collections.Generic;
using System.Linq;
using Material.Icons;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.GridModule.ViewModels;

public class MenuViewModel
    : BindableBase, IDisposable
{
    #region Private Fields

    private readonly Action closeCallback;
    private readonly Action executeCallback;
    private readonly Func<bool> isCheckedGetter;
    private readonly List<SubscriptionToken> subscriptions = [];

    private string header;
    private bool isChecked;
    private bool isDisposed;

    #endregion Private Fields

    #region Public Constructors

    public MenuViewModel(string header, Action executeCallback, Action closeCallback = default,
        Func<bool> isCheckedGetter = default, MaterialIconKind? icon = default)
    {
        this.closeCallback = closeCallback;
        this.executeCallback = executeCallback;
        this.isCheckedGetter = isCheckedGetter;

        Command = new DelegateCommand(ExecuteCallback);
        Header = header;
        Icon = icon;

        Update();
    }

    public MenuViewModel(string header, IEnumerable<BindableBase> menuItems, Action closeCallback,
        MaterialIconKind? icon = default)
        : this(header: header, executeCallback: default, closeCallback: closeCallback, icon: icon)
    {
        MenuItems = menuItems?.ToArray();
    }

    #endregion Public Constructors

    #region Public Properties

    public DelegateCommand Command { get; }

    public bool HasIcon => Icon != default;

    public string Header
    {
        get { return header; }
        set { SetProperty(ref header, value); }
    }

    public MaterialIconKind? Icon { get; }

    // Non nullable variant for the binding of the icon control
    public MaterialIconKind IconKind => Icon ?? default;

    public bool IsChecked
    {
        get { return isChecked; }
        set { SetProperty(ref isChecked, value); }
    }

    public IReadOnlyList<BindableBase> MenuItems { get; }

    #endregion Public Properties

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

    public void Update()
    {
        if (isCheckedGetter != default)
        {
            IsChecked = isCheckedGetter.Invoke();
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

            if (MenuItems != default)
            {
                foreach (var menuItem in MenuItems.OfType<IDisposable>().ToArray())
                {
                    menuItem.Dispose();
                }
            }
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void ExecuteCallback()
    {
        closeCallback?.Invoke();

        executeCallback?.Invoke();

        Update();
    }

    #endregion Private Methods
}
