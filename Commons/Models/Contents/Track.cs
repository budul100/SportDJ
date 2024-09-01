using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Contents
{
    public class Track
    {
        #region Public Constructors

        public Track(string path, string title, string artist, string info, TimeSpan duration)
        {
            Path = path;
            Title = title;
            Artist = artist;
            Info = info;
            Duration = duration;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Artist { get; }

        public Bitmap Bitmap { get; set; }

        public long? Cue { get; set; }

        public TimeSpan Duration { get; set; }

        public long? End { get; set; }

        public string Info { get; }

        public bool IsChecked { get; set; }

        public bool IsSilent { get; set; }

        public string Path { get; }

        public List<Register> Registers { get; } = [];

        public long Start { get; set; }

        public string Title { get; }

        #endregion Public Properties
    }
}