using SportDJ.Services.VolumeService.Enums;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static SportDJ.Services.VolumeService.Extensions.AudioUtilities;

namespace SportDJ.Services.VolumeService.Models
{
    internal sealed class AudioSession
        : IDisposable
    {
        #region Private Fields

        private IAudioSessionControl2 _ctl;
        private Process _process;

        #endregion Private Fields

        #region Public Constructors

        public AudioSession(IAudioSessionControl2 ctl)
        {
            _ctl = ctl;
            VolumeObj = ctl as ISimpleAudioVolume;
        }

        #endregion Public Constructors

        #region Public Properties

        public string DisplayName
        {
            get
            {
                CheckDisposed();
                _ctl.GetDisplayName(out var s);
                return s;
            }
            set
            {
                CheckDisposed();
                _ctl.GetDisplayName(out var s);
                if (s != value)
                {
                    _ctl.SetDisplayName(value, Guid.Empty);
                }
            }
        }

        public Guid GroupingParam
        {
            get
            {
                CheckDisposed();
                _ctl.GetGroupingParam(out var g);
                return g;
            }
            set
            {
                CheckDisposed();
                _ctl.SetGroupingParam(value, Guid.Empty);
            }
        }

        public string IconPath
        {
            get
            {
                CheckDisposed();
                _ctl.GetIconPath(out var s);
                return s;
            }
            set
            {
                CheckDisposed();
                _ctl.GetIconPath(out var s);
                if (s != value)
                {
                    _ctl.SetIconPath(value, Guid.Empty);
                }
            }
        }

        public string Identifier
        {
            get
            {
                CheckDisposed();
                _ctl.GetSessionIdentifier(out var s);
                return s;
            }
        }

        public string InstanceIdentifier
        {
            get
            {
                CheckDisposed();
                _ctl.GetSessionInstanceIdentifier(out var s);
                return s;
            }
        }

        public Process Process
        {
            get
            {
                if (_process == null && ProcessId != 0)
                {
                    try
                    {
                        _process = Process.GetProcessById(ProcessId);
                    }
                    catch
                    {
                        // do nothing
                    }
                }
                return _process;
            }
        }

        public int ProcessId
        {
            get
            {
                CheckDisposed();
                _ctl.GetProcessId(out var i);
                return i;
            }
        }

        public AudioSessionState State
        {
            get
            {
                CheckDisposed();
                _ctl.GetState(out var s);
                return s;
            }
        }

        public ISimpleAudioVolume VolumeObj { get; }

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            if (_ctl != null)
            {
                Marshal.ReleaseComObject(_ctl);

                _ctl = null;
            }
        }

        public override string ToString()
        {
            var s = DisplayName;
            if (!string.IsNullOrEmpty(s))
                return "DisplayName: " + s;

            if (Process != null)
                return "Process: " + Process.ProcessName;

            return "Pid: " + ProcessId;
        }

        #endregion Public Methods

        #region Private Methods

        private void CheckDisposed()
        {
            if (_ctl == null)
                throw new ObjectDisposedException("Control");
        }

        #endregion Private Methods
    }
}