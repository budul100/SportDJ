using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Midi;
using Prism.Events;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Services.MonitorService.Mixxx.Assets;
using SportDJ.Services.MonitorService.Mixxx.Extensions;

namespace SportDJ.Services.MonitorService.Mixxx
{
    public class Service
        : IMonitorService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;

        private CancellationTokenSource cancellationTokenSource;
        private bool isDisposed;
        private int? midiIndex;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            DeployMappingMixxx();
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive { get; private set; }

        public bool IsReady => midiIndex.HasValue;

        public VolumeType Type { get; private set; }

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

        public void Initialize(Device device)
        {
            midiIndex = Constants.MidiMixxxOutputDevice.GetMidiOutIndex();

            eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
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

        // TODO: Implement volume raise for Mixxx
        public void Raise(float? volumeTarget = null, int? milliSeconds = null)
        { }

        // TODO: Implement volume reduction for Mixxx
        public void Reduce(float? volumeTarget = null, int? milliSeconds = null)
        { }

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
                    File.Copy(
                        sourceFileName: sourceFile,
                        destFileName: targetFile);
                }
            }
            catch
            { }
        }

        private async void MuteMixxx(int? milliSeconds, CancellationToken cancellationToken)
        {
            if (IsReady)
            {
                try
                {
                    using var midiOut = new MidiOut(midiIndex.Value);

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

                    Type = VolumeType.Muted;

                    await midiOut.DecksStop(cancellationToken);

                    midiOut.VolumeSetDefault();

                    Type = VolumeType.None;
                }
                finally
                {
                    eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
                }
            }
        }

        #endregion Private Methods
    }
}