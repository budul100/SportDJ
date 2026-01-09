using Avalonia;
using Avalonia.Controls;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Shareds.UserControls
{
    public partial class EntryView
        : UserControl
    {
        #region Private Fields

        private Entry entry;

        #endregion Private Fields

        #region Public Fields

        public static readonly DirectProperty<EntryView, string> CounterProperty = AvaloniaProperty
            .RegisterDirect<EntryView, string>(
                name: nameof(Counter),
                getter: o => GetCounter(o.entry));

        public static readonly DirectProperty<EntryView, string> DurationProperty = AvaloniaProperty
            .RegisterDirect<EntryView, string>(
                name: nameof(Duration),
                getter: o => GetDuration(o.entry));

        public static readonly DirectProperty<EntryView, Entry> EntryProperty = AvaloniaProperty
            .RegisterDirect<EntryView, Entry>(
                name: nameof(Entry),
                getter: o => o.entry,
                setter: (o, v) => o.Entry = v);

        public static readonly DirectProperty<EntryView, Track> TrackProperty = AvaloniaProperty
            .RegisterDirect<EntryView, Track>(
                name: nameof(Track),
                getter: o => o.entry?.Track);

        #endregion Public Fields

        #region Public Constructors

        public EntryView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        public string Counter => GetCounter(entry);

        public string Duration => GetDuration(entry);

        public Entry Entry
        {
            get => entry;

            set
            {
                var oldEntry = entry;
                var oldTrack = Track;
                var oldCounter = Counter;
                var oldDuration = Duration;

                if (SetAndRaise(
                    property: EntryProperty,
                    field: ref entry,
                    value: value))
                {
                    RaisePropertyChanged(
                        property: TrackProperty,
                        oldValue: oldTrack,
                        newValue: Track);

                    RaisePropertyChanged(
                        property: CounterProperty,
                        oldValue: oldCounter,
                        newValue: Counter);

                    RaisePropertyChanged(
                        property: DurationProperty,
                        oldValue: oldDuration,
                        newValue: Duration);
                }
            }
        }

        public Track Track => entry?.Track;

        #endregion Public Properties

        #region Private Methods

        private static string GetCounter(Entry entry)
        {
            return entry?.Track?.Counter > 0
                ? $"({entry?.Track?.Counter})"
                : default;
        }

        private static string GetDuration(Entry entry)
        {
            return entry?.Track?.Duration?
                .ToString(Constants.FormatDuration);
        }

        #endregion Private Methods
    }
}