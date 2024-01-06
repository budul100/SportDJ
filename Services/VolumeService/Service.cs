using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Services.VolumeService.Extensions;
using SportDJ.Services.VolumeService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SportDJ.Services.VolumeService
{
    public class Service
        : IVolumeService
    {
        #region Private Fields

        private readonly string baseProcess;
        private readonly IDispatcherService dispatcherService;

        private IEnumerable<AudioProcess> audioProcesses;
        private CancellationTokenSource cancellationTokenSource;
        private int? deviceNumber;
        private bool isActive;
        private bool isDisposed;
        private float volumeTarget;

        #endregion Private Fields

        #region Public Constructors

        public Service(IDispatcherService dispatcherService)
        {
            this.dispatcherService = dispatcherService;

            baseProcess = AppExtensions.GetProcessName();
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

                var relevantProcesses = GetUnchangedProcesses().ToArray();

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

                if (audioProcesses?.Any(p => p.ProcessName != baseProcess) == true)
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
                        .Where(p => p.ProcessName != baseProcess
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

        public void Set(int deviceNumber)
        {
            Reset();

            this.deviceNumber = deviceNumber;
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
            if (deviceNumber.HasValue)
            {
                var sessions = AudioUtilities.GetAllSessions(
                    deviceNumber: deviceNumber.Value).ToArray();

                var relevants = sessions
                    .Where(s => s.Process != default).ToArray();

                foreach (var relevant in relevants)
                {
                    var processName = relevant.Process.ProcessName;

                    relevant.VolumeObj.GetMasterVolume(
                        pfLevel: out var volume);

                    var result = new AudioProcess(
                        processName: processName,
                        session: relevant,
                        volumeGiven: volume);

                    yield return result;
                }
            }
        }

        private IEnumerable<AudioProcess> GetUnchangedProcesses()
        {
            if (audioProcesses?.Any() == true)
            {
                var relevantProcesses = audioProcesses
                    .Where(p => p.ProcessName != baseProcess).ToArray();

                foreach (var relevantProcess in relevantProcesses)
                {
                    relevantProcess.Session.VolumeObj.GetMasterVolume(
                        pfLevel: out var volume);

                    if (volume == relevantProcess.VolumeLower)
                    {
                        yield return relevantProcess;
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
                    var volumeProcess = volumeCurrent > process.VolumeGiven
                        ? process.VolumeGiven
                        : volumeCurrent;

                    process.Session.VolumeObj.SetMasterVolume(
                        fLevel: volumeProcess,
                        EventContext: Guid.Empty);

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
                    relevantProcess.Session.VolumeObj.SetMasterVolume(
                        fLevel: volumeCurrent,
                        EventContext: Guid.Empty);

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
                    .Where(p => p.ProcessName != baseProcess).ToArray();

                RaiseVolume(
                    processes: relevantProcesses,
                    fadeStep: 1F);
            }
        }

        #endregion Private Methods
    }
}