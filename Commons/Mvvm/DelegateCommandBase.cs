using System;
using System.Threading;
using System.Windows.Input;

namespace SportDJ.Commons.Mvvm;

public abstract class DelegateCommandBase
    : ICommand
{
    #region Private Fields

    // Captured on creation (UI thread), so changes raised from other threads reach the UI thread
    private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;

    #endregion Private Fields

    #region Public Events

    public event EventHandler CanExecuteChanged;

    #endregion Public Events

    #region Public Methods

    bool ICommand.CanExecute(object parameter)
    {
        return CanExecuteCore(parameter);
    }

    void ICommand.Execute(object parameter)
    {
        ExecuteCore(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        var handler = CanExecuteChanged;

        if (handler == default)
        {
            return;
        }

        if (synchronizationContext != default
            && synchronizationContext != SynchronizationContext.Current)
        {
            synchronizationContext.Post(
                d: _ => handler(this, EventArgs.Empty),
                state: default);
        }
        else
        {
            handler(this, EventArgs.Empty);
        }
    }

    #endregion Public Methods

    #region Protected Methods

    protected abstract bool CanExecuteCore(object parameter);

    protected abstract void ExecuteCore(object parameter);

    #endregion Protected Methods
}
