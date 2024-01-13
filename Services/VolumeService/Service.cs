using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Volume;
using SportDJ.Commons.Interfaces;
using SportDJ.Services.VolumeService.Extensions;
using SportDJ.Services.VolumeService.Models;

namespace SportDJ.Services.VolumeService
{
    public class Service
        : IVolumeService
    {
        #region Private Fields

        private readonly int baseProcessId;
        private readonly IEventAggregator eventAggregator;
        private IEnumerable<AudioProcess> audioProcesses;
        private CancellationTokenSource cancellationTokenSource;

        private string deviceName;
        private bool isDisposed;
        private bool isToMute;
        private float volumeTarget;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
            baseProcessId = Environment.ProcessId;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive { get; private set; }

        public VolumeType VolumeType { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Activate()
        {
            audioProcesses = default;

            Task.Run(() => DetermineAudioProcesses());
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
                && VolumeType != VolumeType.Muted
                && VolumeType != VolumeType.None
                && volume > volumeTarget)
            {
                if (VolumeType == VolumeType.Reduce)
                {
                    cancellationTokenSource?.Cancel();
                }

                var relevantProcesses = audioProcesses
                    .Where(p => !p.IsBaseSession
                        && p.Session.SimpleAudioVolume.Volume != p.VolumeGiven).ToArray();

                if (relevantProcesses.Any())
                {
                    volumeTarget = volume
                        ?? Constants.VolumeMax;

                    VolumeType = VolumeType.Raise;

                    var fadeStep = milliSeconds > 0
                        ? Constants.FadeStepInMilliSec / (float)milliSeconds
                        : 1F;

                    cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = cancellationTokenSource.Token;

                    Task.Run(() => RaiseVolume(
                        processes: relevantProcesses,
                        fadeStep: fadeStep,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    VolumeType = VolumeType.None;
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
                && VolumeType != VolumeType.Muted
                && (VolumeType == VolumeType.None || VolumeType == VolumeType.Raise || volume < volumeTarget))
            {
                if (VolumeType == VolumeType.Raise)
                {
                    cancellationTokenSource?.Cancel();
                }

                volumeTarget = volume
                    ?? Constants.VolumeMin;

                VolumeType = VolumeType.Reduce;

                var fadeStep = milliSeconds > 0
                    ? Constants.FadeStepInMilliSec / (float)milliSeconds
                    : 1F;

                var relevantProcesses = audioProcesses
                    .Where(p => !p.IsBaseSession
                        && p.VolumeGiven > volume).ToArray();

                if (relevantProcesses.Any())
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = cancellationTokenSource.Token;

                    Task.Run(() => ReduceVolume(
                        processes: relevantProcesses,
                        fadeStep: fadeStep,
                        cancellationToken: cancellationToken));
                }
            }
        }

        public void Set(string deviceName)
        {
            ResetVolume();

            this.deviceName = deviceName;
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

        private void DetermineAudioProcesses()
        {
            audioProcesses = GetAudioProcesses().ToArray();

            IsActive = audioProcesses?.Any(p => !p.IsBaseSession) == true;

            eventAggregator.GetEvent<VolumeUpdatedEvent>().Publish(this);
        }

        private IEnumerable<AudioProcess> GetAudioProcesses()
        {
            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                var deviceEnumerator = new MMDeviceEnumerator();

                var devices = deviceEnumerator.EnumerateAudioEndPoints(
                    dataFlow: DataFlow.Render,
                    dwStateMask: DeviceState.Active);

                if (devices.Any(d => d.IsMatching(deviceName)))
                {
                    var sessions = devices.Where(d => d.IsMatching(deviceName))
                        .Select(d => d.AudioSessionManager.Sessions).ToArray();

                    foreach (var session in sessions)
                    {
                        var sessionFound = false;

                        for (var index = Constants.IndexAudioSessionMin; index < session.Count; index++)
                        {
                            sessionFound = true;

                            var current = session[index];

                            if (current.GetProcessID.ProcessExists())
                            {
                                var result = new AudioProcess(
                                    session: current,
                                    volumeGiven: current.SimpleAudioVolume.Volume,
                                    isBaseSession: current.GetProcessID == baseProcessId);

                                yield return result;
                            }
                        }

                        if (sessionFound)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        private void RaiseVolume(IEnumerable<AudioProcess> processes, float fadeStep,
            CancellationToken? cancellationToken = default)
        {
            var volumeCurrent = processes.Min(v => v.VolumeLower);

            while (volumeCurrent < volumeTarget
                && cancellationToken?.IsCancellationRequested != true)
            {
                volumeCurrent += fadeStep;

                if (volumeCurrent > volumeTarget)
                {
                    volumeCurrent = volumeTarget;
                }

                foreach (var process in processes)
                {
                    process.Session.SimpleAudioVolume.Volume = volumeCurrent > process.VolumeGiven
                        ? process.VolumeGiven
                        : volumeCurrent;

                    process.VolumeLower = volumeCurrent;
                }

                cancellationToken?.WaitHandle.WaitOne(
                    millisecondsTimeout: Constants.FadeStepInMilliSec);
            }

            VolumeType = volumeTarget < Constants.VolumeMax
                ? VolumeType.Lowered
                : VolumeType.None;
        }

        private void ReduceVolume(IEnumerable<AudioProcess> processes, float fadeStep,
            CancellationToken cancellationToken)
        {
            var volumeCurrent = processes.Max(v => v.VolumeGiven);

            while (volumeCurrent > volumeTarget
                && !cancellationToken.IsCancellationRequested)
            {
                volumeCurrent -= fadeStep;

                if (volumeCurrent < volumeTarget)
                {
                    volumeCurrent = volumeTarget;
                }

                var relevantProcesses = processes
                    .Where(p => p.VolumeGiven > volumeCurrent).ToArray();

                foreach (var relevantProcess in relevantProcesses)
                {
                    relevantProcess.Session.SimpleAudioVolume.Volume = volumeCurrent;

                    relevantProcess.VolumeLower = volumeCurrent;
                }

                cancellationToken.WaitHandle.WaitOne(
                    millisecondsTimeout: Constants.FadeStepInMilliSec);
            }

            VolumeType = isToMute
                ? VolumeType.Muted
                : VolumeType.Lowered;
        }

        private void ResetVolume()
        {
            if (IsActive)
            {
                volumeTarget = Constants.VolumeMax;
                isToMute = false;

                VolumeType = VolumeType.Raise;

                var relevantProcesses = audioProcesses
                    .Where(p => !p.IsBaseSession).ToArray();

                RaiseVolume(
                    processes: relevantProcesses,
                    fadeStep: 1F);

                IsActive = false;

                eventAggregator.GetEvent<VolumeUpdatedEvent>().Publish(this);
            }
        }

        #endregion Private Methods
    }
}