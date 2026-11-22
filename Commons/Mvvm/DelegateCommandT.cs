using System;

namespace SportDJ.Commons.Mvvm;

public class DelegateCommand<T>(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
    : DelegateCommandBase
{
    #region Private Fields

    private readonly Func<T, bool> canExecute = canExecuteMethod ?? throw new ArgumentNullException(nameof(canExecuteMethod));
    private readonly Action<T> execute = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));

    #endregion Private Fields

    #region Public Constructors

    public DelegateCommand(Action<T> executeMethod)
        : this(executeMethod, _ => true)
    { }

    #endregion Public Constructors

    #region Public Methods

    public bool CanExecute(T parameter)
    {
        return canExecute(parameter);
    }

    public void Execute(T parameter)
    {
        execute(parameter);
    }

    #endregion Public Methods

    #region Protected Methods

    protected override bool CanExecuteCore(object parameter)
    {
        return CanExecute(Convert(parameter));
    }

    protected override void ExecuteCore(object parameter)
    {
        Execute(Convert(parameter));
    }

    #endregion Protected Methods

    #region Private Methods

    private static T Convert(object parameter)
    {
        return parameter is T typed
            ? typed
            : default;
    }

    #endregion Private Methods
}
