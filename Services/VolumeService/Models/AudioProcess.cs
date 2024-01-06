namespace SportDJ.Services.VolumeService.Models
{
    internal class AudioProcess
    {
        #region Public Constructors

        public AudioProcess(string processName, AudioSession session, float volumeGiven)
        {
            ProcessName = processName;
            Session = session;
            VolumeGiven = volumeGiven;
        }

        #endregion Public Constructors

        #region Public Properties

        public string ProcessName { get; }

        public AudioSession Session { get; }

        public float VolumeGiven { get; }

        public float VolumeLower { get; set; }

        #endregion Public Properties
    }
}