using System;

namespace SportDJ.Commons.Models.Contents;

public class Entry(Track track, Action<Entry> selector)
{
    #region Public Properties

    public bool IsPlayed { get; set; }

    public bool IsSelected { get; set; }

    public Action Selector => () => selector.Invoke(this);

    public Track Track { get; } = track;

    #endregion Public Properties
}