using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NAudio.Midi;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.ControllerService.Assets;
using SportDJ.Services.ControllerService.Exceptions;
using SportDJ.Services.ControllerService.Extensions;
using SportDJ.Services.ControllerService.Records;

namespace SportDJ.Services.ControllerService;

public class Service
    : IControllerService
{
    #region Private Fields

    private readonly IEventAggregator eventAggregator;
    private readonly ConcurrentDictionary<Key, (StrokeType Type, Action Action)> keyStrokes = [];
    private readonly IEnumerable<Profile> profiles;
    private readonly IStorageService<Session> sessionService;

    private bool isDisposed;
    private TaskCompletionSource<Key> listenerTcs;
    private MidiIn midiIn;
    private MidiOut midiOut;
    private byte[] sysExFinal;
    private byte[] sysExInit;

    #endregion Private Fields

    #region Public Constructors

    public Service(IEventAggregator eventAggregator, IStorageService<Session> sessionService)
    {
        this.eventAggregator = eventAggregator;
        this.sessionService = sessionService;

        eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
            action: InitializeController,
            keepSubscriberReferenceAlive: true);

        eventAggregator?.GetEvent<SettingsLoadedEvent<Session>>().Subscribe(
            action: InitializeController,
            keepSubscriberReferenceAlive: true);

        profiles = GetProfiles().ToArray();

        Update();
    }

    #endregion Public Constructors

    #region Public Properties

    public Controller Controller { get; private set; }

    public IEnumerable<Controller> Controllers { get; private set; } = [];

    public bool IsActive => midiIn != default;

    #endregion Public Properties

    #region Public Methods

    public void Colorize(Key key, int colorValue)
    {
        if (midiOut != default)
        {
            switch (key.Type)
            {
                case KeyType.Note:

                    // Send Note On with velocity = colorValue (often used for LED color)
                    var noteOnStatus = 0x90 | ((key.Channel - 1) & 0x0F); // Note On status byte (NAudio channels are 1-based)
                    var noteOnData1 = key.Number;          // Note number
                    var noteOnData2 = colorValue;          // Velocity/color

                    var noteOnMessage = (noteOnStatus << 16) | (noteOnData1 << 8) | noteOnData2;

                    midiOut.Send(noteOnMessage);
                    break;

                case KeyType.Control:

                    // Send Control Change with value = colorValue
                    var ccStatus = 0xB0 | ((key.Channel - 1) & 0x0F); // Control Change status byte (NAudio channels are 1-based)
                    var ccData1 = key.Number;              // Controller number
                    var ccData2 = colorValue;              // Value/color

                    var ccMessage = (ccStatus << 16) | (ccData1 << 8) | ccData2;

                    midiOut.Send(ccMessage);
                    break;
            }
        }
    }

    public void Dispose()
    {
        Dispose(
            disposing: true);

        GC.SuppressFinalize(
            obj: this);
    }

    public async Task<Key> ListenAsync(CancellationToken cancellationToken)
    {
        CloseListener();

        var tcs = new TaskCompletionSource<Key>(
            creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);

        listenerTcs = tcs;

        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                // A cancelled listener must not remove the listener of its successor
                Interlocked.CompareExchange(
                    location1: ref listenerTcs,
                    value: default,
                    comparand: tcs);
            }
        }
    }

    public void Register(Key key, StrokeType strokeType, Action action)
    {
        keyStrokes[key] = (Type: strokeType, Action: action);
    }

    public void Set(string name)
    {
        var controller = default(Controller);

        if (!string.IsNullOrWhiteSpace(name))
        {
            if (Controllers.Any())
            {
                controller = Controllers.SingleOrDefault(d => d.Name == name);
            }
            else
            {
                throw new NoControllerAvailableException();
            }
        }

        if (controller != Controller)
        {
            CloseController();

            SetController(controller);

            sessionService.Contents.ControllerName = controller?.Name;

            sessionService.Save();
        }

        eventAggregator.GetEvent<ControllerUpdatedEvent>().Publish();
    }

    public void Unregister(Key key)
    {
        keyStrokes.TryRemove(
            key: key,
            value: out _);
    }

    public void Update()
    {
        Controllers = GetControllers().ToArray();

        eventAggregator?.GetEvent<ControllersUpdatedEvent>().Publish();
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                CloseListener();
                CloseController();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static IEnumerable<Controller> GetControllers()
    {
        for (var indexIn = 0; indexIn < MidiIn.NumberOfDevices; indexIn++)
        {
            var deviceIn = MidiIn.DeviceInfo(indexIn);
            var indexOut = GetIndexOut(deviceIn);

            var result = new Controller(
                Name: deviceIn.ProductName,
                IndexIn: indexIn,
                IndexOut: indexOut);

            yield return result;
        }
    }

    private static int? GetIndexOut(MidiInCapabilities deviceIn)
    {
        for (var index = 0; index < MidiOut.NumberOfDevices; index++)
        {
            var deviceOut = MidiOut.DeviceInfo(index);

            if (deviceOut.ProductName == deviceIn.ProductName)
            {
                return index;
            }
        }

        return default;
    }

    private static IEnumerable<Profile> GetProfiles()
    {
        var sourceFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Constants.ControllerProfilesSource,
            Constants.ControllerProfilesFile);

        var serializer = new XmlSerializer(typeof(ControllerProfiles));

        using var file = new StreamReader(sourceFile);

        var profiles = default(ControllerProfiles);

        try
        {
            profiles = serializer.Deserialize(file) as ControllerProfiles;
        }
        catch
        {
            // TODO: Log exception
            Debugger.Break();
        }

        if (profiles?.Profiles?.Count > 0)
        {
            foreach (var profile in profiles.Profiles)
            {
                yield return profile;
            }
        }
    }

    private void CloseController()
    {
        try
        {
            midiIn?.Stop();

            if (midiOut != default)
            {
                if (sysExFinal?.Length > 0)
                {
                    midiOut.SendBuffer(sysExFinal);
                }

                midiOut.Dispose();
                midiOut = default;
            }

            if (midiIn != default)
            {
                midiIn.MessageReceived -= OnMessageReceived;

                midiIn.Dispose();
                midiIn = default;
            }
        }
        catch
        {
            // TODO: Log exception
        }
        finally
        {
            sysExInit = default;
            sysExFinal = default;

            Controller = default;
        }
    }

    private void CloseListener()
    {
        listenerTcs?.TrySetCanceled();
        listenerTcs = default;
    }

    private void InitializeController()
    {
        var name = sessionService?.Contents?.ControllerName;

        try
        {
            Set(name);
        }
        catch (Exception)
        { }
    }

    private void OnKeyPressed(Key key, StrokeType type)
    {
        var tcs = listenerTcs;

        if (tcs != default)
        {
            // A release must not be learned as the key
            if (type == StrokeType.Down)
            {
                tcs.TrySetResult(key);
            }
        }
        else if (keyStrokes.TryGetValue(
            key: key,
            value: out var registered)
            && registered.Type == type)
        {
            registered.Action?.Invoke();
        }
    }

    private void OnMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is MidiEvent midiOnEvent
            && midiOnEvent.CommandCode == MidiCommandCode.NoteOn
            && midiOnEvent is NoteOnEvent noteOnEvent)
        {
            var key = new Key(
                Type: KeyType.Note,
                Channel: noteOnEvent.Channel,
                Number: noteOnEvent.NoteNumber);

            // Many controllers send Note On with velocity 0 instead of Note Off
            OnKeyPressed(
                key: key,
                type: noteOnEvent.Velocity > 0
                    ? StrokeType.Down
                    : StrokeType.Up);
        }
        else if (e.MidiEvent is MidiEvent midiOffEvent
            && midiOffEvent.CommandCode == MidiCommandCode.NoteOff
            && midiOffEvent is NoteEvent noteOffEvent)
        {
            var key = new Key(
                Type: KeyType.Note,
                Channel: noteOffEvent.Channel,
                Number: noteOffEvent.NoteNumber);

            OnKeyPressed(
                key: key,
                type: StrokeType.Up);
        }
        else if (e.MidiEvent is ControlChangeEvent controlEvent)
        {
            var key = new Key(
                Type: KeyType.Control,
                Channel: controlEvent.Channel,
                Number: (int)controlEvent.Controller);

            var type = controlEvent.ControllerValue > 0
                ? StrokeType.Down
                : StrokeType.Up;

            OnKeyPressed(
                key: key,
                type: type);
        }
    }

    private void SetController(Controller controller)
    {
        if (controller != default)
        {
            try
            {
                var profile = profiles.GetProfile(controller);

                if (profile != default)
                {
                    sysExInit = profile.InitSysEx?.Parse();
                    sysExFinal = profile.FinalSysEx?.Parse();
                }

                midiIn = new MidiIn(controller.IndexIn);

                midiIn.MessageReceived += OnMessageReceived;

                midiIn.Start();

                if (controller.IndexOut.HasValue)
                {
                    midiOut = new MidiOut(controller.IndexOut.Value);

                    if (sysExInit?.Length > 0)
                    {
                        midiOut.SendBuffer(sysExInit);
                    }
                }

                Controller = controller;
            }
            catch (Exception exception)
            {
                // Most common reason: the MIDI input is already opened by another application (WinMM is exclusive)
                Trace.WriteLine($"[Controller] Opening '{controller.Name}' failed: {exception}");

                CloseController();
            }
        }
    }

    #endregion Private Methods
}
