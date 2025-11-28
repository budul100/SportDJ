using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace SportDJ.Commons.Models.Contents
{
    public class Track(string path, string file)
    {
        #region Public Properties

        public string Artist { get; set; }

        public Bitmap Bitmap { get; set; }

        public float? BPM { get; set; }

        public int Counter => Registers?.Count ?? 0;

        public long? Cue { get; set; }

        public TimeSpan? Duration { get; set; }

        public long? End { get; set; }

        public string File { get; } = file;

        public string Info { get; set; }

        public bool IsRendered { get; set; }

        public bool IsSilent { get; set; }

        public bool IsTrimmed { get; set; }

        public bool IsUpdated { get; set; }

        public string Path { get; } = path;

        public List<Register> Registers { get; } = [];

        public long Start { get; set; }

        public string Title { get; set; }

        #endregion Public Properties
    }
}