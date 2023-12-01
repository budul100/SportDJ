using Score2Stream.Commons;

namespace SportDJ.Commons.Models.Settings
{
    public class Output
    {
        #region Public Properties

        public string DeviceName { get; set; }

        public string Name { get; set; }

        public float Volume { get; set; } = Constants.VolumeMax;

        public double VolumePortionBackground { get; set; } = Constants.VolumePortionBackgroundDefault;

        #endregion Public Properties
    }
}