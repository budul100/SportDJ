using System;
using System.Threading;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.KeyboardService;

public class Service
    : IKeyboardService
{
    #region Private Fields

    private readonly Lock listeningLock = new();

    private object listener;

    #endregion Private Fields

    #region Public Events

    public event EventHandler Changed = delegate { };

    #endregion Public Events

    #region Public Properties

    public bool IsAny
    {
        get
        {
            lock (listeningLock)
            {
                return listener != default;
            }
        }
    }

    #endregion Public Properties

    #region Public Methods

    public void Begin(object owner)
    {
        if (owner != default)
        {
            lock (listeningLock)
            {
                listener = owner;
            }

            OnChanged();
        }
    }

    public void End(object owner)
    {
        var hasChanged = false;

        lock (listeningLock)
        {
            if (listener != default
                && ReferenceEquals(listener, owner))
            {
                listener = default;
                hasChanged = true;
            }
        }

        if (hasChanged)
        {
            OnChanged();
        }
    }

    public bool IsListening(object owner)
    {
        lock (listeningLock)
        {
            return owner != default
                && ReferenceEquals(listener, owner);
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void OnChanged()
    {
        Changed?.Invoke(
            sender: this,
            e: EventArgs.Empty);
    }

    #endregion Private Methods
}
