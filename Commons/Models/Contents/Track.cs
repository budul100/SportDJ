using System;

namespace SportDJ.Commons.Models.Contents
{
    public class Track
    {
        #region Public Properties

        public string Artist { get; }

        public int Counter { get; }

        public TimeSpan Length { get; }

        public string Path { get; }

        public TimeSpan Start { get; }

        public string Title { get; }

        #endregion Public Properties
    }
}