using NAudio.Wave;
using System;

namespace SportDJ.Commons.Interfaces
{
    public interface ISignalService
        : IDisposable
    {
        #region Public Properties

        TimeSpan FadeDuration { get; set; }

        bool IsLoop { get; set; }

        bool IsPlaying { get; }

        bool IsStarted { get; }

        ISampleProvider Output { get; }

        TimeSpan Position { get; }

        TimeSpan Remaining { get; }

        #endregion Public Properties

        #region Public Methods

        void End();

        void Pause();

        void Start();

        void Stop();

        #endregion Public Methods
    }
}