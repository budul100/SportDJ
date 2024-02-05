using System;

namespace SportDJ.Commons.Models.Contents
{
    public class Entry
    {
        #region Private Fields

        private readonly Action<Entry> selector;

        #endregion Private Fields

        #region Public Constructors

        public Entry(Track track, Action<Entry> selector)
        {
            this.selector = selector;

            Track = track;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsPlayed { get; set; }

        public bool IsSelected { get; set; }

        public Action Selector => () => selector.Invoke(this);

        public Track Track { get; }

        #endregion Public Properties
    }
}