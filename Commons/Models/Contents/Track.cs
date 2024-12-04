using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace SportDJ.Commons.Models.Contents
{
    public class Track(string path, string title, string artist, string info,
        TimeSpan duration, int? bpm)
    {
        #region Public Properties

        public string Artist { get; } = artist;

        public Bitmap Bitmap { get; set; }

        public int? Bpm { get; } = bpm;

        public long? Cue { get; set; }

        public TimeSpan Duration { get; set; } = duration;

        public long? End { get; set; }

        public string Info { get; } = info;

        public bool IsChecked { get; set; }

        public bool IsSilent { get; set; }

        public string Path { get; } = path;

        public List<Register> Registers { get; } = [];

        public long Start { get; set; }

        public string Title { get; } = title;

        #endregion Public Properties
    }
}