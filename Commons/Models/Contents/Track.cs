using System;

namespace SportDJ.Commons.Models.Contents
{
    public class Track
    {
        #region Public Constructors

        public Track(string path, string title, string artist, TimeSpan length)
        {
            Artist = artist;
            Length = length;
            Path = path;
            Title = title;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Artist { get; }

        public bool Checked { get; set; }

        public TimeSpan Length { get; set; }

        public Track Next { get; set; }

        public string Path { get; }

        public Track Previous { get; set; }

        public TimeSpan Start { get; set; }

        public string Title { get; }

        public bool Trimmed { get; set; }

        #endregion Public Properties
    }
}