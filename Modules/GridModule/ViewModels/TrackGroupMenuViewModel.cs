using System;
using System.Linq;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;

namespace SportDJ.Modules.GridModule.ViewModels;

public class TrackGroupMenuViewModel
    : MenuViewModel
{
    #region Public Constructors

    public TrackGroupMenuViewModel(IEventAggregator eventAggregator, TrackMenuViewModel[] menuItems,
        Action closeCallback)
        : base(header: GetHeader(menuItems), menuItems: menuItems, closeCallback: closeCallback)
    {
        AddSubscription(eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
            action: _ => UpdateSelection(),
            keepSubscriberReferenceAlive: true));
    }

    #endregion Public Constructors

    #region Private Methods

    private static string GetHeader(TrackMenuViewModel[] menuItems)
    {
        var result = default(string);

        if (menuItems?.Length > 0)
        {
            var first = menuItems[0].Entry.Track?.Info ?? string.Empty;
            var last = menuItems[^1].Entry.Track?.Info ?? string.Empty;

            // Zip stops at the shorter text, so there is no index out of range
            var commonLength = first.Zip(last)
                .TakeWhile(pair => pair.First == pair.Second).Count();

            var length = commonLength + Constants.MenuGroupTextLength;

            var firstHeader = first.Length > length
                ? $"{first[..length]}..."
                : first;

            var lastHeader = last.Length > length
                ? $"{last[..length]}..."
                : last;

            result = $"Tracks {firstHeader} - {lastHeader}";
        }

        return result;
    }

    #endregion Private Methods
}
