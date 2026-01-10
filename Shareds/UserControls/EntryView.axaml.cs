using Avalonia;
using Avalonia.Controls;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Shareds.UserControls
{
    public partial class EntryView
        : UserControl
    {
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

        public static readonly DirectProperty<EntryView, IEventAggregator> EventAggregatorProperty = AvaloniaProperty
            .RegisterDirect<EntryView, IEventAggregator>(
                name: nameof(EventAggregator),
                getter: o => o.eventAggregator,
                setter: (o, v) => o.EventAggregator = v);

        public static readonly DirectProperty<EntryView, string> InfoProperty = AvaloniaProperty
            .RegisterDirect<EntryView, string>(
                name: nameof(Info),
                getter: o => o.entry?.Track?.Info);

        public static readonly DirectProperty<EntryView, bool> IsPlayedProperty = AvaloniaProperty
            .RegisterDirect<EntryView, bool>(
                name: nameof(IsPlayed),
                getter: o => o.entry?.IsPlayed ?? false);

        public static readonly DirectProperty<EntryView, bool> IsSelectedProperty = AvaloniaProperty
            .RegisterDirect<EntryView, bool>(
                name: nameof(IsSelected),
                getter: o => o.entry?.IsSelected ?? false);

        #endregion Public Fields

        #region Private Fields

        private Entry entry;
        private IEventAggregator eventAggregator;

        #endregion Private Fields

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
                var oldInfo = Info;
                var oldCounter = Counter;
                var oldDuration = Duration;
                var oldIsPlayed = IsPlayed;
                var oldIsSelected = IsSelected;

                if (SetAndRaise(
                    property: EntryProperty,
                    field: ref entry,
                    value: value))
                {
                    RaisePropertyChanged(
                        property: InfoProperty,
                        oldValue: oldInfo,
                        newValue: Info);

                    RaisePropertyChanged(
                        property: CounterProperty,
                        oldValue: oldCounter,
                        newValue: Counter);

                    RaisePropertyChanged(
                        property: DurationProperty,
                        oldValue: oldDuration,
                        newValue: Duration);

                    RaisePropertyChanged(
                        property: IsPlayedProperty,
                        oldValue: oldIsPlayed,
                        newValue: IsPlayed);

                    RaisePropertyChanged(
                        property: IsSelectedProperty,
                        oldValue: oldIsSelected,
                        newValue: IsSelected);

                    SubscribeEvents();
                }
            }
        }

        public IEventAggregator EventAggregator
        {
            get => eventAggregator;

            set
            {
                if (SetAndRaise(
                    property: EventAggregatorProperty,
                    field: ref eventAggregator,
                    value: value))
                {
                    SubscribeEvents();
                }
            }
        }

        public string Info => entry?.Track?.Info;

        public bool IsPlayed => entry?.IsPlayed ?? false;

        public bool IsSelected => entry?.IsSelected ?? false;

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

        private void SubscribeEvents()
        {
            if (eventAggregator != default && entry != default)
            {
                eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                    action: _ => UpdateEntry(),
                    threadOption: ThreadOption.UIThread,
                    keepSubscriberReferenceAlive: true,
                    filter: e => e == entry);

                eventAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
                    action: _ => UpdateTrack(),
                    threadOption: ThreadOption.UIThread,
                    keepSubscriberReferenceAlive: true,
                    filter: t => t == entry?.Track);
            }
        }

        private void UpdateEntry()
        {
            var oldIsPlayed = IsPlayed;
            var oldIsSelected = IsSelected;

            RaisePropertyChanged(
                property: IsPlayedProperty,
                oldValue: oldIsPlayed,
                newValue: IsPlayed);

            RaisePropertyChanged(
                property: IsSelectedProperty,
                oldValue: oldIsSelected,
                newValue: IsSelected);
        }

        private void UpdateTrack()
        {
            var oldInfo = Info;
            var oldDuration = Duration;
            var oldCounter = Counter;

            RaisePropertyChanged(
                property: InfoProperty,
                oldValue: oldInfo,
                newValue: Info);

            RaisePropertyChanged(
                property: DurationProperty,
                oldValue: oldDuration,
                newValue: Duration);

            RaisePropertyChanged(
                property: CounterProperty,
                oldValue: oldCounter,
                newValue: Counter);
        }

        #endregion Private Methods
    }
}