using System;
using System.Linq;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class SettingsViewModel
    : TabViewModel
{
    #region Private Fields

    private const string KeyboardHint = "Press a key...";
    private const string MidiHint = "Press a MIDI key...";
    private const string NoKey = "None";
    private const string Titel = "Settings";

    private Player player;

    #endregion Private Fields

    #region Public Constructors

    public SettingsViewModel(IEventAggregator eventAggregator, IKeyListenService keyListenService)
    {
        Colors = Enum.GetValues<PlayerColorType>()
            .Select(c => new ColorOptionViewModel(c)).ToArray();

        SelectColorCommand = new DelegateCommand<PlayerColorType>(SelectColor);

        ListenKeyboardCommand = new DelegateCommand(ToggleKeyboard);
        ClearKeyboardCommand = new DelegateCommand(ClearKeyboard);
        ListenMidiCommand = new DelegateCommand(ToggleMidi);
        ClearMidiCommand = new DelegateCommand(ClearMidi);

        eventAggregator?.GetEvent<PlayerUpdateEvent>().Subscribe(
            action: _ => Refresh(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: p => player != default
                && player == p);

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

    public ColorOptionViewModel[] Colors { get; }

    public bool IsActive => player != default;

    public bool IsListeningKeyboard => Service?.IsListeningKeyboard == true;

    public bool IsListeningMidi => Service?.IsListeningMidi == true;

    public bool IsOverlay
    {
        get => Service?.IsOverlay == true;

        set
        {
            if (Service != default)
            {
                Service.IsOverlay = value;
            }
        }
    }

    public bool IsReset
    {
        get => Service?.ResetPosition == true;

        set
        {
            if (Service != default)
            {
                Service.ResetPosition = value;
            }
        }
    }

    public string KeyboardButtonText => IsListeningKeyboard
        ? "Cancel"
        : "Assign";

    public string KeyboardText => IsListeningKeyboard
        ? KeyboardHint
        : Service?.KeyCode.GetKey() ?? NoKey;

    public DelegateCommand ListenKeyboardCommand { get; }

    public DelegateCommand ListenMidiCommand { get; }

    public string MidiButtonText => IsListeningMidi
        ? "Cancel"
        : "Assign";

    public string MidiText => IsListeningMidi
        ? MidiHint
        : Service?.Key.GetText() ?? NoKey;

    public string Name
    {
        get => Service?.Name;

        set
        {
            if (Service != default)
            {
                Service.Name = value;
            }
        }
    }

    public DelegateCommand<PlayerColorType> SelectColorCommand { get; }

    public override string Title => Titel;

    #endregion Public Properties

    #region Private Properties

    private IPlayerService Service => player?.PlayerService;

    #endregion Private Properties

    #region Public Methods

    public void Set(Player player)
    {
        if (player != default
            && this.player != player)
        {
            // Pending assignments belong to the previous player
            Service?.CancelListening();

            this.player = player;

            Refresh();
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void ClearKeyboard()
    {
        if (Service != default)
        {
            Service.KeyCode = default;
        }
    }

    private void ClearMidi()
    {
        Service?.Unregister();
    }

    private void Refresh()
    {
        // The bindings still show the values of the previous player otherwise
        RaisePropertyChanged(nameof(IsActive));
        RaisePropertyChanged(nameof(IsListeningKeyboard));
        RaisePropertyChanged(nameof(IsListeningMidi));
        RaisePropertyChanged(nameof(IsOverlay));
        RaisePropertyChanged(nameof(IsReset));
        RaisePropertyChanged(nameof(KeyboardButtonText));
        RaisePropertyChanged(nameof(KeyboardText));
        RaisePropertyChanged(nameof(MidiButtonText));
        RaisePropertyChanged(nameof(MidiText));
        RaisePropertyChanged(nameof(Name));

        var color = Service?.Color ?? default;

        foreach (var option in Colors)
        {
            option.IsSelected = option.ColorType == color;
        }
    }

    private void SelectColor(PlayerColorType colorType)
    {
        if (Service != default)
        {
            Service.Color = colorType;
        }
    }

    private void ToggleKeyboard()
    {
        if (Service?.IsListeningKeyboard == true)
        {
            Service.CancelListening();
        }
        else
        {
            Service?.ListenKeyboard();
        }
    }

    private void ToggleMidi()
    {
        if (Service?.IsListeningMidi == true)
        {
            Service.CancelListening();
        }
        else if (Service != default)
        {
            _ = Service.RegisterAsync();
        }
    }

    #endregion Private Methods
}
