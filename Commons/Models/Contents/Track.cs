using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace SportDJ.Commons.Models.Contents;

public class Track(string path, string file)
{
    #region Public Properties

    public string Artist { get; set; }

    public Bitmap Bitmap { get; set; }

    public float? BPM { get; set; }

    public int Counter { get; set; }

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

    #region Public Operators

    public static bool operator !=(Track left, Track right)
    {
        return !(left == right);
    }

    public static bool operator ==(Track left, Track right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    #endregion Public Operators

    #region Public Methods

    public override bool Equals(object obj)
    {
        return Equals(obj as Track);
    }

    public bool Equals(Track other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && string.Equals(File, other.File, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Artist, other.Artist, StringComparison.Ordinal)
            && string.Equals(Title, other.Title, StringComparison.Ordinal)
            && string.Equals(Info, other.Info, StringComparison.Ordinal)
            && BPM == other.BPM
            && Counter == other.Counter
            && Cue == other.Cue
            && Duration == other.Duration
            && End == other.End
            && Start == other.Start;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Path?.ToLowerInvariant());
        hash.Add(File?.ToLowerInvariant());
        hash.Add(Artist);
        hash.Add(Title);
        hash.Add(Info);
        hash.Add(BPM);
        hash.Add(Counter);
        hash.Add(Cue);
        hash.Add(Duration);
        hash.Add(End);
        hash.Add(Start);

        return hash.ToHashCode();
    }

    #endregion Public Methods
}