using System;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Modules.GridModule.ViewModels;

public class TrackMenuViewModel
    : MenuViewModel
{
    #region Public Constructors

    public TrackMenuViewModel(IEventAggregator eventAggregator, Entry entry, Action closeCallback)
        : base(header: default, executeCallback: entry.Selector, closeCallback: closeCallback)
    {
        Entry = entry;

        AddSubscription(eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
            action: _ => UpdateItem(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: e => e == entry));

        AddSubscription(eventAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
            action: _ => UpdateTrack(),
            threadOption: ThreadOption.PublisherThread,
            keepSubscriberReferenceAlive: true,
            filter: t => t == entry.Track));

        UpdateTrack();
    }

    #endregion Public Constructors

    #region Public Properties

    public Entry Entry { get; }

    #endregion Public Properties

    #region Private Methods

    private void UpdateItem()
    {
        IsPlayed = Entry.IsPlayed;
        IsSelected = Entry.IsSelected;
    }

    private void UpdateTrack()
    {
        var track = Entry.Track;

        var duration = track?.Duration?.ToString(Constants.FormatDuration);

        Header = $"{track?.Info} ({duration}) [{track?.Counter}]";

        UpdateItem();
    }

    #endregion Private Methods
}
