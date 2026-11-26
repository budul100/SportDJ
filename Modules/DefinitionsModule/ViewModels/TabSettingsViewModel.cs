using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class TabSettingsViewModel
    : TabViewModel
{
    #region Private Fields

    private const string KeyboardHint = "Press a key...";
    private const string MidiHint = "Press a MIDI key...";
    private const string NoKey = "None";
    private const string Titel = "Settings";

    private readonly IGridService gridService;

    private Grid tab;

    #endregion Private Fields

    #region Public Constructors

    public TabSettingsViewModel(IEventAggregator eventAggregator, IGridService gridService,
        IKeyboardService keyListenService)
    {
        this.gridService = gridService;

        ListenKeyboardCommand = new DelegateCommand(ToggleKeyboard);
        ClearKeyboardCommand = new DelegateCommand(ClearKeyboard);
        ListenMidiCommand = new DelegateCommand(ToggleMidi);
        ClearMidiCommand = new DelegateCommand(ClearMidi);

        eventAggregator?.GetEvent<TabUpdatedEvent>().Subscribe(
            action: _ => Refresh(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: g => tab != default
                && tab == g);

        if (keyListenService != default)
        {
            keyListenService.Changed += (_, _) => Refresh();
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public static int MaxLengthName => Constants.NameMaxLength;

    public DelegateCommand ClearKeyboardCommand { get; }

    public DelegateCommand ClearMidiCommand { get; }

    public Grid Current => tab;

    public bool HasKey => !IsListeningKeyboard
        && !string.IsNullOrWhiteSpace(KeyCode);

    public bool IsActive => tab != default;

    public bool IsListeningKeyboard => gridService?.IsListening(tab) == true;

    public bool IsListeningMidi => gridService?.IsListeningMidi(tab) == true;

    public string KeyboardText => IsListeningKeyboard
        ? KeyboardHint
        : NoKey;

    public string KeyCode => tab?.KeyCode;

    public DelegateCommand ListenKeyboardCommand { get; }

    public DelegateCommand ListenMidiCommand { get; }

    public string MidiText => IsListeningMidi
        ? MidiHint
        : tab?.Key.GetText() ?? NoKey;

    public string Name
    {
        get => tab?.Name;

        set => gridService?.SetName(tab, value);
    }

    public override string Title => Titel;

    #endregion Public Properties

    #region Public Methods

    // Pending assignments are dropped, e.g. if the pane gets closed
    public void Cancel()
    {
        gridService?.CancelListening();
    }

    // Shows the stored name again, e.g. if the input was left empty
    public void RestoreName()
    {
        RaisePropertyChanged(nameof(Name));
    }

    public void Set(Grid tab)
    {
        if (this.tab != tab)
        {
            // Pending assignments belong to the previous tab
            gridService?.CancelListening();

            this.tab = tab;

            Refresh();
        }
    }

    // Drops the tab, if it is not part of the grid anymore
    public void Validate()
    {
        if (tab != default
            && !tab.IsAttached())
        {
            Set(default);
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void ClearKeyboard()
    {
        gridService?.SetKeyCode(tab, default);
    }

    private void ClearMidi()
    {
        gridService?.ClearMidi(tab);
    }

    private void Refresh()
    {
        // The bindings still show the values of the previous tab otherwise
        RaisePropertyChanged(nameof(HasKey));
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsListeningKeyboard));
        RaisePropertyChanged(nameof(IsListeningMidi));
        RaisePropertyChanged(nameof(KeyboardText));
        RaisePropertyChanged(nameof(KeyCode));
        RaisePropertyChanged(nameof(MidiText));
        RaisePropertyChanged(nameof(Name));
    }

    private void ToggleKeyboard()
    {
        if (gridService?.IsListening(tab) == true)
        {
            gridService.CancelListening();
        }
        else
        {
            gridService?.ListenKey(tab);
        }
    }

    private void ToggleMidi()
    {
        if (gridService?.IsListeningMidi(tab) == true)
        {
            gridService.CancelListening();
        }
        else
        {
            gridService?.ListenMidi(tab);
        }
    }

    #endregion Private Methods
}
