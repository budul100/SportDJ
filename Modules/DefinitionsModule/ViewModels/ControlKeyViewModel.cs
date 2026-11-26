using System;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

/// <summary>
/// One assignable key of the controls (e.g. pause), shown as one row in the settings of the controls.
/// </summary>
public class ControlKeyViewModel
    : BindableBase
{
    #region Private Fields

    private const string KeyboardHint = "Press a key...";
    private const string NoKey = "None";

    private readonly Func<string> getKeyCode;
    private readonly IKeyboardService keyboardService;
    private readonly Action<string> setKeyCode;

    #endregion Private Fields

    #region Public Constructors

    public ControlKeyViewModel(string title, Func<string> getKeyCode, Action<string> setKeyCode,
        IKeyboardService keyboardService)
    {
        this.getKeyCode = getKeyCode ?? throw new ArgumentNullException(nameof(getKeyCode));
        this.keyboardService = keyboardService;
        this.setKeyCode = setKeyCode ?? throw new ArgumentNullException(nameof(setKeyCode));

        Title = title;

        ListenCommand = new DelegateCommand(
            executeMethod: ToggleListening);

        ClearCommand = new DelegateCommand(
            executeMethod: Clear);
    }

    #endregion Public Constructors

    #region Public Properties

    public DelegateCommand ClearCommand { get; }

    public bool HasKey => !IsListening
        && !string.IsNullOrWhiteSpace(KeyCode);

    public bool IsListening => keyboardService?.IsListening(this) == true;

    public string KeyboardText => IsListening
        ? KeyboardHint
        : NoKey;

    public string KeyCode => getKeyCode();

    public DelegateCommand ListenCommand { get; }

    public string Title { get; }

    #endregion Public Properties

    #region Public Methods

    // Takes the key, the assignment is ended by the caller
    public void Assign(string keyCode)
    {
        setKeyCode(keyCode);

        Refresh();
    }

    public void Cancel()
    {
        keyboardService?.End(this);

        Refresh();
    }

    public void Refresh()
    {
        RaisePropertyChanged(nameof(HasKey));
        RaisePropertyChanged(nameof(IsListening));
        RaisePropertyChanged(nameof(KeyboardText));
        RaisePropertyChanged(nameof(KeyCode));
    }

    #endregion Public Methods

    #region Private Methods

    private void Clear()
    {
        // An empty text instead of null, otherwise the default key comes back after loading
        setKeyCode(string.Empty);

        Cancel();
    }

    private void ToggleListening()
    {
        if (IsListening)
        {
            Cancel();
        }
        else
        {
            keyboardService?.Begin(this);

            Refresh();
        }
    }

    #endregion Private Methods
}
