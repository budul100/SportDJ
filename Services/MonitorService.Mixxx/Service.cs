using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Midi;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Services.MonitorService.Mixxx.Assets;
using SportDJ.Services.MonitorService.Mixxx.Extensions;

namespace SportDJ.Services.MonitorService.Mixxx;

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
            // A running fade is replaced by the new one
            cancellationTokenSource?.Cancel();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _ = Task.Run(() => MuteMixxx(
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

    // A task instead of async void, so exceptions cannot crash the application
    private async Task MuteMixxx(int? milliSeconds, CancellationToken cancellationToken)
    {
        if (IsReady)
        {
            try
            {
                using var midiOut = new MidiOut(midiIndex.Value);

                if (milliSeconds is null
                    || milliSeconds <= Constants.MidiVolumeMax)
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
            catch (OperationCanceledException)
            {
                // Replaced by a newer fade or disposed
            }
            catch (Exception)
            {
                // TODO: Log exception, the MIDI device might have been removed
            }
            finally
            {
                eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
            }
        }
    }

    #endregion Private Methods
}