namespace SportDJ.Commons.Models.Contents
{
    public class Entry
    {
        #region Public Constructors

        public Entry(Track track)
        {
            Track = track;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsPlayed { get; set; }

        public bool IsSelected { get; set; }

        public Track Track { get; }

        #endregion Public Properties
    }
}