using NAudio.CoreAudioApi;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Services.VolumeService.Extensions;
using SportDJ.Services.VolumeService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SportDJ.Services.VolumeService
{
    public class Service
        : IVolumeService
    {
        #region Private Fields

        private readonly int baseProcessId;
        private readonly IDispatcherService dispatcherService;
        private IEnumerable<AudioProcess> audioProcesses;
        private CancellationTokenSource cancellationTokenSource;
        private string deviceName;
        private bool isDisposed;
        private float volumeTarget;

        #endregion Private Fields

        #region Public Constructors

        public Service(IDispatcherService dispatcherService)
        {
            this.dispatcherService = dispatcherService;

            baseProcessId = Process.GetCurrentProcess().Id;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive { get; private set; }

        public VolumeType VolumeType { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            if (IsActive)
            {
                Reset();
            }

            IsActive = false;
        }

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Raise(float? volume = default, int? milliSeconds = default)
        {
            if (VolumeType != VolumeType.None
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

                    dispatcherService.InvokeAsync(() => RaiseVolume(
                        processes: relevantProcesses,
                        fadeStep: fadeStep,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    VolumeType = VolumeType.None;

                    audioProcesses = default;
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
                && (VolumeType == VolumeType.None || VolumeType == VolumeType.Raise || volume < volumeTarget))
            {
                if (VolumeType == VolumeType.None || audioProcesses?.Any() == true)
                {
                    audioProcesses = GetAudioProcesses().ToArray();
                }

                if (audioProcesses?.Any(p => !p.IsBaseSession) == true)
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

                        dispatcherService.InvokeAsync(() => ReduceVolume(
                            processes: relevantProcesses,
                            fadeStep: fadeStep,
                            cancellationToken: cancellationToken));
                    }
                }
            }
        }

        public void Set(string deviceName)
        {
            Reset();

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
                    Reset();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private IEnumerable<AudioProcess> GetAudioProcesses()
        {
            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                var deviceEnumerator = new MMDeviceEnumerator();

                var devices = deviceEnumerator.EnumerateAudioEndPoints(
                    dataFlow: DataFlow.Render,
                    dwStateMask: DeviceState.Active);

                if (devices.Any(d => d.FriendlyName == deviceName))
                {
                    var sessions = devices.FirstOrDefault(d => d.FriendlyName == deviceName)?
                        .AudioSessionManager.Sessions;

                    if (sessions?.Count > 1)
                    {
                        for (var index = 0; index < sessions.Count; index++)
                        {
                            var session = sessions[index];

                            if (session.GetProcessID.ProcessExists())
                            {
                                var result = new AudioProcess(
                                    session: session,
                                    volumeGiven: session.SimpleAudioVolume.Volume,
                                    isBaseSession: session.GetProcessID == baseProcessId);

                                yield return result;
                            }
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

            audioProcesses = default;
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

            VolumeType = volumeTarget > Constants.VolumeMin
                ? VolumeType.Lowered
                : VolumeType.Muted;
        }

        private void Reset()
        {
            if (audioProcesses?.Any() == true)
            {
                volumeTarget = Constants.VolumeMax;

                VolumeType = VolumeType.Raise;

                var relevantProcesses = audioProcesses
                    .Where(p => !p.IsBaseSession).ToArray();

                RaiseVolume(
                    processes: relevantProcesses,
                    fadeStep: 1F);
            }
        }

        #endregion Private Methods
    }
}