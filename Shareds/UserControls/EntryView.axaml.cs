using Avalonia;
using Avalonia.Controls;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Shareds.UserControls;

public partial class EntryView
    : UserControl
{
    #region Public Fields

    public static readonly DirectProperty<EntryView, string> CounterProperty = AvaloniaProperty
        .RegisterDirect<EntryView, string>(
            name: nameof(Counter),
            getter: o => o.counter);

    public static readonly DirectProperty<EntryView, string> DurationProperty = AvaloniaProperty
        .RegisterDirect<EntryView, string>(
            name: nameof(Duration),
            getter: o => o.duration);

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
            getter: o => o.info);

    public static readonly DirectProperty<EntryView, bool> IsPlayedProperty = AvaloniaProperty
        .RegisterDirect<EntryView, bool>(
            name: nameof(IsPlayed),
            getter: o => o.isPlayed);

    public static readonly DirectProperty<EntryView, bool> IsSelectedProperty = AvaloniaProperty
        .RegisterDirect<EntryView, bool>(
            name: nameof(IsSelected),
            getter: o => o.isSelected);

    #endregion Public Fields

    #region Private Fields

    private string counter;
    private string duration;
    private Entry entry;
    private SubscriptionToken entryUpdatedToken;
    private IEventAggregator eventAggregator;
    private string info;
    private bool isAttached;
    private bool isPlayed;
    private bool isSelected;

    // The aggregator the tokens belong to, the property may already point to a new one when unsubscribing
    private IEventAggregator subscribedAggregator;

    private SubscriptionToken trackUpdatedToken;

    #endregion Private Fields

    #region Public Constructors

    public EntryView()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Public Properties

    public string Counter => counter;

    public string Duration => duration;

    public Entry Entry
    {
        get => entry;

        set
        {
            if (SetAndRaise(
                property: EntryProperty,
                field: ref entry,
                value: value))
            {
                RefreshEntry();
                RefreshTrack();

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

    public string Info => info;

    public bool IsPlayed => isPlayed;

    public bool IsSelected => isSelected;

    #endregion Public Properties

    #region Protected Methods

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        isAttached = true;

        // Events may have been missed while detached
        RefreshEntry();
        RefreshTrack();

        SubscribeEvents();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        isAttached = false;

        UnsubscribeEvents();

        base.OnDetachedFromVisualTree(e);
    }

    #endregion Protected Methods

    #region Private Methods

    private static string GetCounter(Entry entry)
    {
        return entry?.Track?.Counter > 0
            ? $"({entry.Track.Counter})"
            : default;
    }

    private static string GetDuration(Entry entry)
    {
        return entry?.Track?.Duration?
            .ToString(Constants.FormatDuration);
    }

    private void RefreshEntry()
    {
        SetAndRaise(
            property: IsPlayedProperty,
            field: ref isPlayed,
            value: entry?.IsPlayed ?? false);

        SetAndRaise(
            property: IsSelectedProperty,
            field: ref isSelected,
            value: entry?.IsSelected ?? false);
    }

    private void RefreshTrack()
    {
        SetAndRaise(
            property: InfoProperty,
            field: ref info,
            value: entry?.Track?.Info);

        SetAndRaise(
            property: DurationProperty,
            field: ref duration,
            value: GetDuration(entry));

        SetAndRaise(
            property: CounterProperty,
            field: ref counter,
            value: GetCounter(entry));
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        if (!isAttached
            || eventAggregator == default
            || entry == default)
        {
            return;
        }

        // Captured locally, so a later change of the property does not affect this subscription
        var currentEntry = entry;

        subscribedAggregator = eventAggregator;

        entryUpdatedToken = subscribedAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
            action: _ =>
            {
                RefreshEntry();
                RefreshTrack();
            },
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: e => e == currentEntry);

        trackUpdatedToken = subscribedAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
            action: _ => RefreshTrack(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true,
            filter: t => t == currentEntry.Track);
    }

    private void UnsubscribeEvents()
    {
        if (subscribedAggregator == default)
        {
            return;
        }

        subscribedAggregator.GetEvent<EntryUpdatedEvent>().Unsubscribe(entryUpdatedToken);
        subscribedAggregator.GetEvent<TrackUpdatedEvent>().Unsubscribe(trackUpdatedToken);

        entryUpdatedToken = default;
        trackUpdatedToken = default;

        subscribedAggregator = default;
    }

    #endregion Private Methods
}
