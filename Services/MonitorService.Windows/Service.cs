using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DryIoc.ImTools;
using NAudio.CoreAudioApi;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Services.MonitorService.Windows.Extensions;
using SportDJ.Services.MonitorService.Windows.Models;

namespace SportDJ.Services.MonitorService.Windows;

public class Service(IEventAggregator eventAggregator)
    : IMonitorService
{
    #region Private Fields

    private readonly int baseProcessId = Environment.ProcessId;

    private CancellationTokenSource cancellationTokenSource;
    private Device device;
    private bool isDisposed;
    private bool isToMute;
    private float volumeTarget;

    #endregion Private Fields

    #region Public Properties

    public bool IsActive { get; private set; }

    public bool IsReady => device != default;

    public IEnumerable<Session> Sessions { get; private set; }

    public VolumeType Type { get; private set; }

    #endregion Public Properties

    #region Public Methods

    public void Activate()
    {
        Sessions = default;

        Task.Run(DetermineSessions);
    }

    public void Deactivate()
    {
        ResetVolume();
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
        ResetVolume();

        this.device = device?.Type == DeviceType.Wasapi
            ? device
            : default;

        eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
    }

    public void Mute(int? milliSeconds = default)
    {
        if (IsActive)
        {
            isToMute = true;

            Reduce(
                volume: Constants.VolumeMin,
                milliSeconds: milliSeconds);
        }
    }

    public void Raise(float? volume = default, int? milliSeconds = default)
    {
        if (volume.HasValue
            && (volume < Constants.VolumeMin || volume > Constants.VolumeMax))
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        if (IsActive
            && Type != VolumeType.Muted
            && Type != VolumeType.None
            && volume > volumeTarget)
        {
            if (Type == VolumeType.Reduce)
            {
                cancellationTokenSource?.Cancel();
            }

            var relevants = Sessions?
                .Where(p => p.VolumeCurrent != p.VolumeInitial).ToArray();

            if (relevants?.Length > 0)
            {
                volumeTarget = volume
                    ?? Constants.VolumeMax;

                Type = VolumeType.Raise;

                var fadeStep = milliSeconds > 0
                    ? Constants.FadeStepInMilliSec / (float)milliSeconds
                    : 1F;

                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                Task.Run(() => RaiseVolume(
                    sessions: relevants,
                    fadeStep: fadeStep,
                    cancellationToken: cancellationToken));
            }
            else
            {
                Type = VolumeType.None;
            }
        }
    }

    public void Reduce(float? volume = default, int? milliSeconds = default)
    {
        if (volume.HasValue
            && (volume < Constants.VolumeMin || volume > Constants.VolumeMax))
        {
            throw new ArgumentOutOfRangeException(nameof(volume));
        }

        if (IsActive
            && Type != VolumeType.Muted
            && (Type == VolumeType.None || Type == VolumeType.Raise || volume < volumeTarget))
        {
            if (Type == VolumeType.Raise)
            {
                cancellationTokenSource?.Cancel();
            }

            volumeTarget = volume
                ?? Constants.VolumeMin;

            Type = VolumeType.Reduce;

            var fadeStep = milliSeconds > 0
                ? Constants.FadeStepInMilliSec / (float)milliSeconds
                : 1F;

            var relevants = Sessions?
                .Where(p => p.VolumeInitial > volume).ToArray();

            if (relevants?.Length > 0)
            {
                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                Task.Run(() => ReduceVolume(
                    sessions: relevants,
                    fadeStep: fadeStep,
                    cancellationToken: cancellationToken));
            }
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
                ResetVolume();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void DetermineSessions()
    {
        Sessions = GetSessions().ToArray();

        IsActive = Sessions?.Count() > 0;

        eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
    }

    private IEnumerable<Session> GetSessions()
    {
        if (device != default)
        {
            var deviceEnumerator = new MMDeviceEnumerator();

            var devices = deviceEnumerator.EnumerateAudioEndPoints(
                dataFlow: DataFlow.Render,
                dwStateMask: DeviceState.Active);

            if (devices.Any(d => d.IsMatching(device.Name)))
            {
                var sessions = devices
                    .Where(d => d.IsMatching(device.Name))
                    .Select(d => d.AudioSessionManager.Sessions).ToArray();

                foreach (var session in sessions)
                {
                    for (var index = Constants.IndexAudioSessionMin; index < session.Count; index++)
                    {
                        var current = session[index];

                        if (current.GetProcessID.ProcessExists()
                            && current.GetProcessID != baseProcessId)
                        {
                            var result = new Session(
                                control: current,
                                volumeInitial: current.SimpleAudioVolume.Volume);

                            yield return result;
                        }
                    }
                }
            }
        }
    }

    private void RaiseVolume(IEnumerable<Session> sessions, float fadeStep,
        CancellationToken? cancellationToken)
    {
        if (sessions?.Count() > 0)
        {
            var volumeCurrent = sessions.Min(v => v.VolumeLower);

            while (volumeCurrent < volumeTarget
                && cancellationToken?.IsCancellationRequested != true)
            {
                volumeCurrent += fadeStep;

                if (volumeCurrent > volumeTarget)
                {
                    volumeCurrent = volumeTarget;
                }

                foreach (var process in sessions)
                {
                    process.VolumeCurrent = volumeCurrent > process.VolumeInitial
                        ? process.VolumeInitial
                        : volumeCurrent;

                    process.VolumeLower = volumeCurrent;
                }

                cancellationToken?.WaitHandle.WaitOne(
                    millisecondsTimeout: Constants.FadeStepInMilliSec);
            }
        }

        Type = volumeTarget < Constants.VolumeMax
            ? VolumeType.Lowered
            : VolumeType.None;
    }

    private void ReduceVolume(IEnumerable<Session> sessions, float fadeStep,
        CancellationToken cancellationToken)
    {
        if (sessions?.Count() > 0)
        {
            var volumeCurrent = sessions.Max(v => v.VolumeInitial);

            while (volumeCurrent > volumeTarget
                && !cancellationToken.IsCancellationRequested)
            {
                volumeCurrent -= fadeStep;

                if (volumeCurrent < volumeTarget)
                {
                    volumeCurrent = volumeTarget;
                }

                var relevantProcesses = sessions
                    .Where(p => p.VolumeInitial > volumeCurrent).ToArray();

                foreach (var relevantProcess in relevantProcesses)
                {
                    relevantProcess.VolumeCurrent = volumeCurrent;
                    relevantProcess.VolumeLower = volumeCurrent;
                }

                cancellationToken.WaitHandle.WaitOne(
                    millisecondsTimeout: Constants.FadeStepInMilliSec);
            }
        }

        Type = isToMute
            ? VolumeType.Muted
            : VolumeType.Lowered;
    }

    private void ResetVolume()
    {
        if (IsActive)
        {
            cancellationTokenSource?.Cancel();

            volumeTarget = Constants.VolumeMax;
            isToMute = false;

            Type = VolumeType.Raise;

            RaiseVolume(
                sessions: Sessions,
                fadeStep: 1F,
                cancellationToken: default);

            IsActive = false;

            eventAggregator.GetEvent<MonitorUpdatedEvent>().Publish(this);
        }
    }

    #endregion Private Methods
}