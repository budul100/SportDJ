using NAudio.Midi;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Interfaces;
using SportDJ.Services.MidiService.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SportDJ.Services.MidiService
{
    public class Service
        : IMidiService
    {
        #region Private Fields

        private CancellationTokenSource cancellationTokenSource;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service()
        {
            DeployMappingMixxx();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Dispose()
        {
            Dispose(
                disposing: true);
            GC.SuppressFinalize(
                obj: this);
        }

        public void Mute(int? milliSeconds = default)
        {
            if (IsActive)
            {
                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                Task.Run(() => MuteMixxx(
                    milliSeconds: milliSeconds,
                    cancellationToken: cancellationToken));
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    cancellationTokenSource?.Cancel();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void DeployMappingMixxx()
        {
            var sourceFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                Constants.MidiMixxxMappingSource,
                Constants.MidiMixxxMappingFile);

            var targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Constants.MidiMixxxMappingTarget);

            var targetFile = Path.Combine(
                targetFolder,
                Constants.MidiMixxxMappingFile);

            try
            {
                if (Directory.Exists(targetFolder)
                    && !File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                }
            }
            catch
            { }
        }

        private static int GetMidiOutputIndex(string productName)
        {
            for (var index = 0; index < MidiOut.NumberOfDevices; index++)
            {
                if (MidiOut.DeviceInfo(index).ProductName == productName)
                {
                    return index;
                }
            }

            return Constants.MidiOutputNotFound;
        }

        private static async void MuteMixxx(int? milliSeconds, CancellationToken cancellationToken)
        {
            var midiOutIndex = GetMidiOutputIndex(Constants.MidiMixxxOutputDevice);

            if (midiOutIndex >= 0)
            {
                try
                {
                    using var midiOut = new MidiOut(midiOutIndex);

                    if (milliSeconds <= Constants.MidiVolumeMax)
                    {
                        midiOut.VolumeMute();
                    }
                    else
                    {
                        await midiOut.VolumeFadeOut(
                            milliSeconds: milliSeconds,
                            cancellationToken: cancellationToken);
                    }

                    await midiOut.DecksStop(cancellationToken);

                    midiOut.VolumeSetDefault();
                }
                catch
                { }
            }
        }

        #endregion Private Methods
    }
}