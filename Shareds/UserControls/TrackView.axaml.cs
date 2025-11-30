using System;
using Avalonia;
using Avalonia.Controls;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Shareds.UserControls
{
    public partial class TrackView
        : UserControl
    {
        #region Public Fields

        public static readonly DirectProperty<TrackView, string> CounterProperty = AvaloniaProperty
            .RegisterDirect<TrackView, string>(
                name: nameof(Counter),
                getter: t => GetCounter(t.Track));

        public static readonly DirectProperty<TrackView, string> DurationProperty = AvaloniaProperty
            .RegisterDirect<TrackView, string>(
                name: nameof(Duration),
                getter: t => GetDuration(t.Track));

        public static readonly DirectProperty<TrackView, Track> TrackProperty = AvaloniaProperty
            .RegisterDirect<TrackView, Track>(
                name: nameof(Track),
                getter: o => o.Track,
                setter: (o, v) => o.Track = v);

        #endregion Public Fields

        #region Public Constructors

        public TrackView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        public string Counter => GetCounter(Track);

        public string Duration => GetDuration(Track);

        public Track Track
        {
            get => GetValue(TrackProperty);

            set
            {
                SetValue(
                    property: TrackProperty,
                    value: value);

                RaisePropertyChanged(
                    property: CounterProperty,
                    oldValue: default,
                    newValue: Counter);

                RaisePropertyChanged(
                    property: DurationProperty,
                    oldValue: default,
                    newValue: Duration);
            }
        }

        #endregion Public Properties

        #region Private Methods

        private static string GetCounter(Track track)
        {
            return track?.Counter > 0
                ? $"({track.Counter})"
                : default;
        }

        private static string GetDuration(Track track)
        {
            return track?.Duration?
                .ToString(Constants.FormatDuration);
        }

        #endregion Private Methods
    }
}