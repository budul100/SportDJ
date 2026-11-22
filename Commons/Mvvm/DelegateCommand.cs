using System;

namespace SportDJ.Commons.Mvvm;

public class DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
    : DelegateCommandBase
{
    #region Private Fields

    private readonly Func<bool> canExecute = canExecuteMethod ?? throw new ArgumentNullException(nameof(canExecuteMethod));
    private readonly Action execute = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));

    #endregion Private Fields

    #region Public Constructors

    public DelegateCommand(Action executeMethod)
        : this(executeMethod, () => true)
    { }

    #endregion Public Constructors

    #region Public Methods

    public bool CanExecute()
    {
        return canExecute();
    }

    public void Execute()
    {
        execute();
    }

    #endregion Public Methods

    #region Protected Methods

    protected override bool CanExecuteCore(object parameter)
    {
        return CanExecute();
    }

    protected override void ExecuteCore(object parameter)
    {
        Execute();
    }

    #endregion Protected Methods
}
