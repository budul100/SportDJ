using System.Threading;
using System.Threading.Tasks;
using NAudio.Midi;
using SportDJ.Services.MonitorService.Mixxx.Assets;

namespace SportDJ.Services.MonitorService.Mixxx.Extensions;

internal static class MidiExtensions
{
    #region Public Methods

    public static async Task DecksStop(this MidiOut midiOut, CancellationToken cancellationToken)
    {
        for (var deck = 1; deck <= Constants.MidiMixxxDeckCount; deck++)
        {
            var message = new NoteOnEvent(
                absoluteTime: 0,
                channel: Constants.MidiChannel,
                noteNumber: Constants.MidiNoteBase + deck,
                velocity: 0,
                duration: 0).GetAsShortMessage();

            midiOut.Send(message);

            await Task.Delay(
                millisecondsDelay: Commons.Assets.Constants.MidiDelayStopDecks,
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public static int? GetMidiOutIndex(this string productName)
    {
        for (var index = 0; index < MidiOut.NumberOfDevices; index++)
        {
            if (MidiOut.DeviceInfo(index).ProductName == productName)
            {
                return index;
            }
        }

        return default;
    }

    public static async Task VolumeFadeOut(this MidiOut midiOut, int? milliSeconds,
        CancellationToken cancellationToken)
    {
        var delayPerStep = (milliSeconds ?? 0) / (Constants.MidiVolumeMax + 1);

        for (var volume = Constants.MidiVolumeMax; volume >= 0; volume--)
        {
            var message = new ControlChangeEvent(
                absoluteTime: 0,
                channel: Constants.MidiChannel,
                controller: (MidiController)Constants.MidiNoteVolume,
                controllerValue: volume).GetAsShortMessage();

            midiOut.Send(message);

            await Task.Delay(
                millisecondsDelay: delayPerStep,
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public static void VolumeMute(this MidiOut midiOut)
    {
        var message = new ControlChangeEvent(
            absoluteTime: 0,
            channel: Constants.MidiChannel,
            controller: (MidiController)Constants.MidiNoteVolume,
            controllerValue: Constants.MidiVolumeMin).GetAsShortMessage();

        midiOut.Send(message);
    }

    public static void VolumeSetDefault(this MidiOut midiOut)
    {
        var message = new NoteOnEvent(
            absoluteTime: 0,
            channel: Constants.MidiChannel,
            noteNumber: Constants.MidiNoteBase,
            velocity: 1,
            duration: 0).GetAsShortMessage();

        midiOut.Send(message);
    }

    #endregion Public Methods
}