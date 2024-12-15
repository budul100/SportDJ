using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;
using System;

namespace SportDJ.Modules.SearchModule.DesignTime
{
    public class TrackViewModel(string title, string artist, TimeSpan? duration = default)
        : ViewModels.TrackViewModel(default)
    {
        #region Private Fields

        private readonly TimeSpan duration = duration ?? new TimeSpan(
            hours: 00,
            minutes: 1,
            seconds: 15);

        #endregion Private Fields

        #region Public Constructors

        public TrackViewModel()
            : this("Title", "Artist")
        { }

        #endregion Public Constructors

        #region Public Properties

        public override Bitmap Bitmap => new(@".\Additionals\Samples\Sample1.png");

        public override string Content => $"{title} - {artist}";

        public override string Counter => "(1)";

        public override string Duration => duration.ToString(Constants.FormatDuration);

        #endregion Public Properties
    }
}