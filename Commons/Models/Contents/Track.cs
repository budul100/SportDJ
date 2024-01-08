using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Contents
{
    public class Track
    {
        #region Public Constructors

        public Track(string path, string title, string artist, string info, TimeSpan length)
        {
            Artist = artist;
            Info = info;
            Length = length;
            Path = path;
            Title = title;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Artist { get; }

        public Bitmap Bitmap { get; set; }

        public bool Checked { get; set; }

        public string Info { get; }

        public TimeSpan Length { get; set; }

        public string Path { get; }

        public List<Register> Registers { get; } = new List<Register>();

        public TimeSpan Start { get; set; }

        public string Title { get; }

        public bool Trimmed { get; set; }

        #endregion Public Properties
    }
}