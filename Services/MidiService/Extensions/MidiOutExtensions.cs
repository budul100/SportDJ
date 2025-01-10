using System.Threading;
using System.Threading.Tasks;
using NAudio.Midi;
using SportDJ.Commons.Assets;

namespace SportDJ.Services.MidiService.Extensions
{
    internal static class MidiOutExtensions
    {
        #region Public Methods

        public static async Task DecksStop(this MidiOut midiOut, CancellationToken cancellationToken)
        {
            for (var deck = 1; deck <= Constants.MidiMixxxDeckCount; deck++)
            {
                var message = MidiMessage.StartNote(
                    note: Constants.MidiNoteBase + deck,
                    volume: 0,
                    channel: Constants.MidiChannel).RawData;

                midiOut.Send(message);

                await Task.Delay(
                    millisecondsDelay: Constants.MidiDelayStopDecks,
                    cancellationToken: cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public static async Task VolumeFadeOut(this MidiOut midiOut, int? milliSeconds, CancellationToken cancellationToken)
        {
            var delayPerStep = (milliSeconds ?? 0) / (Constants.MidiVolumeMax + 1);

            for (int volume = Constants.MidiVolumeMax; volume >= 0; volume--)
            {
                var message = MidiMessage.ChangeControl(
                    controller: 0x07,
                    value: volume,
                    channel: Constants.MidiChannel).RawData;

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
            var message = MidiMessage.ChangeControl(
                controller: Constants.MidiNoteVolume,
                value: 0,
                channel: Constants.MidiChannel).RawData;

            midiOut.Send(message);
        }

        public static void VolumeSetDefault(this MidiOut midiOut)
        {
            var message = MidiMessage.StartNote(
                note: Constants.MidiNoteBase,
                volume: 1,
                channel: Constants.MidiChannel).RawData;

            midiOut.Send(message);
        }

        #endregion Public Methods
    }
}