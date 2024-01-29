using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class MenuViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly Action callback;

        private string header;
        private bool isPlayed;
        private bool isSelected;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IEnumerable<MenuViewModel> menuItems)
        {
            MenuItems = new ObservableCollection<MenuViewModel>(menuItems);
            Command = new DelegateCommand(ExecuteCallback);

            Header = GetHeader();
        }

        public MenuViewModel(IContainerProvider containerProvider, Entry entry, Action callback)
        {
            this.callback = callback;

            Entry = entry;
            Command = new DelegateCommand(ExecuteCallback);

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: _ => UpdateEntry(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: e => e == entry);

            eventAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => t == entry.Track);

            UpdateTrack();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler CloseMenuEvent;

        #endregion Public Events

        #region Public Properties

        public DelegateCommand Command { get; }

        public Entry Entry { get; }

        public string Header
        {
            get { return header; }
            set { SetProperty(ref header, value); }
        }

        public bool IsPlayed
        {
            get { return isPlayed; }
            set { SetProperty(ref isPlayed, value); }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        public ObservableCollection<MenuViewModel> MenuItems { get; }

        #endregion Public Properties

        #region Private Methods

        private void ExecuteCallback()
        {
            callback?.Invoke();

            CloseMenuEvent?.Invoke(
                sender: this,
                e: default);
        }

        private string GetHeader()
        {
            var first = MenuItems.First().Entry.Track.Info;
            var last = MenuItems.Last().Entry.Track.Info;

            var length = string.Concat(first.TakeWhile((c, i) => c == last[i])).Length + Constants.MenuGroupAdd;

            var firstHeader = first.Length > length
                ? $"{first[..length]}..."
                : first;

            var lastHeader = last.Length > length
                ? $"{last[..length]}..."
                : last;

            return $"Tracks {firstHeader} - {lastHeader}";
        }

        private void UpdateEntry()
        {
            IsPlayed = Entry.Played;
            IsSelected = Entry.Selected;
        }

        private void UpdateTrack()
        {
            var duration = Entry.Track?.Length.ToString(Constants.FormatDuration);

            Header = $"{Entry.Track.Info} ({duration}) [{Entry.Track.Registers.Count}]";

            UpdateEntry();
        }

        #endregion Private Methods
    }
}