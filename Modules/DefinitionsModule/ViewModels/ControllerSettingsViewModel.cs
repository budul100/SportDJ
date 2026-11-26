using System;
using System.Linq;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

/// <summary>
/// Tab for the keys of the controls (pause, talk, screen change).
/// </summary>
public class ControllerSettingsViewModel
    : TabViewModel
{
    #region Private Fields

    private const string Titel = "Controls";

    private readonly IDispatcherService dispatcherService;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageService<Set> settingsService;

    #endregion Private Fields

    #region Public Constructors

    public ControllerSettingsViewModel(IEventAggregator eventAggregator, IStorageService<Set> settingsService,
        IKeyboardService keyboardService, IDispatcherService dispatcherService)
    {
        this.dispatcherService = dispatcherService;
        this.eventAggregator = eventAggregator;
        this.settingsService = settingsService;

        Keys =
        [
            new ControlKeyViewModel(
                title: "Pause output",
                getKeyCode: () => Options?.KeyCodeOutputPause,
                setKeyCode: k => SetKeyCode(o => o.KeyCodeOutputPause = k),
                keyboardService: keyboardService),

            new ControlKeyViewModel(
                title: "Talk (lowers the volume while pressed)",
                getKeyCode: () => Options?.KeyCodeVolumeTalk,
                setKeyCode: k => SetKeyCode(o => o.KeyCodeVolumeTalk = k),
                keyboardService: keyboardService),

            new ControlKeyViewModel(
                title: "Switch full screen",
                getKeyCode: () => Options?.KeyCodeScreenChange,
                setKeyCode: k => SetKeyCode(o => o.KeyCodeScreenChange = k),
                keyboardService: keyboardService),
        ];

        // Only the entry which waits for an assignment takes the key, the entries are bound to the UI
        eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
            action: OnKeyPressed,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: _ => Keys.Any(k => k.IsListening));

        // The options are replaced when a session is loaded
        eventAggregator?.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
            action: Refresh,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
            action: Refresh,
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);

        if (keyboardService != default)
        {
            keyboardService.Changed += (_, _) => Refresh();
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public ControlKeyViewModel[] Keys { get; }

    public override string Title => Titel;

    #endregion Public Properties

    #region Private Properties

    private Options Options => settingsService?.Contents?.Options;

    #endregion Private Properties

    #region Public Methods

    // Pending assignments are dropped, e.g. if the pane gets closed
    public void Cancel()
    {
        foreach (var key in Keys)
        {
            key.Cancel();
        }
    }

    #endregion Public Methods

    #region Private Methods

    private void OnKeyPressed(string keyCode)
    {
        var listening = Keys.FirstOrDefault(k => k.IsListening);

        if (listening != default)
        {
            listening.Assign(keyCode);

            // Ends later, so other receivers of this key press still see the listening state
            dispatcherService?.Post(listening.Cancel);
        }
    }

    private void Refresh()
    {
        foreach (var key in Keys)
        {
            key.Refresh();
        }
    }

    private void SetKeyCode(Action<Options> assign)
    {
        var options = Options;

        if (options != default)
        {
            assign(options);

            settingsService.Save();

            // The controls read the keys from the options
            eventAggregator?.GetEvent<SettingsUpdatedEvent<Set>>().Publish();
        }
    }

    #endregion Private Methods
}
