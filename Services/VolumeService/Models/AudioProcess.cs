using NAudio.CoreAudioApi;

namespace SportDJ.Services.VolumeService.Models
{
    internal class AudioProcess
    {
        #region Public Constructors

        public AudioProcess(AudioSessionControl session, float volumeGiven, bool isBaseSession)
        {
            Session = session;
            VolumeGiven = volumeGiven;
            IsBaseSession = isBaseSession;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsBaseSession { get; }

        public AudioSessionControl Session { get; }

        public float VolumeGiven { get; }

        public float VolumeLower { get; set; }

        #endregion Public Properties
    }
}