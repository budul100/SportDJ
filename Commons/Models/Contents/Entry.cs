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

        public bool Played { get; set; }

        public bool Selected { get; set; }

        public Track Track { get; }

        #endregion Public Properties
    }
}