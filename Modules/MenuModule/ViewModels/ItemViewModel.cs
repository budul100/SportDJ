using Material.Icons;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.MenuModule.ViewModels
{
    public class ItemViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly Action executeCallback;
        private readonly Func<bool> isCheckedGetter;

        private string header;
        private bool isChecked;
        private bool isPlayed;
        private bool isSelected;

        #endregion Private Fields

        #region Public Constructors

        public ItemViewModel(string header, Action executeCallback, Action closeCallback = default,
            Func<bool> isCheckedGetter = default, MaterialIconKind? icon = default)
        {
            this.executeCallback = executeCallback;
            this.isCheckedGetter = isCheckedGetter;

            Command = new DelegateCommand(ExecuteCallback);
            Header = header;
            Icon = icon;

            MenuCloseEvent += (s, e) => closeCallback?.Invoke();

            Update();
        }

        public ItemViewModel(string header, IEnumerable<BindableBase> menuItems, Action closeCallback,
            MaterialIconKind? icon = default)
        {
            Command = new DelegateCommand(ExecuteCallback);
            Header = header;
            Icon = icon;

            MenuItems = new ObservableCollection<BindableBase>(menuItems);

            MenuCloseEvent += (s, e) => closeCallback?.Invoke();

            UpdateItems();
        }

        public ItemViewModel(IEventAggregator eventAggregator, IEnumerable<BindableBase> menuItems, Action closeCallback)
        {
            eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: _ => UpdateItems(),
                keepSubscriberReferenceAlive: true);

            Command = new DelegateCommand(ExecuteCallback);
            Header = GetHeader(menuItems);

            MenuItems = new ObservableCollection<BindableBase>(menuItems);
            MenuCloseEvent += (s, e) => closeCallback?.Invoke();

            UpdateItems();
        }

        public ItemViewModel(IEventAggregator eventAggregator, Entry entry, Action closeCallback)
        {
            executeCallback = entry.Selector;

            eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: _ => UpdateItem(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: e => e == entry);

            eventAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => t == entry.Track);

            Entry = entry;
            Command = new DelegateCommand(ExecuteCallback);

            MenuCloseEvent += (s, e) => closeCallback?.Invoke();

            UpdateTrack();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        #endregion Public Events

        #region Public Properties

        public DelegateCommand Command { get; }

        public Entry Entry { get; }

        public bool HasIcon => Icon != default;

        public string Header
        {
            get { return header; }
            set { SetProperty(ref header, value); }
        }

        public MaterialIconKind? Icon { get; }

        public bool IsChecked
        {
            get { return isChecked; }
            set { SetProperty(ref isChecked, value); }
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

        public ObservableCollection<BindableBase> MenuItems { get; }

        public ItemViewModel Selected { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Update()
        {
            if (isCheckedGetter != default)
            {
                IsChecked = isCheckedGetter.Invoke();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static string GetHeader(IEnumerable<BindableBase> menuItems)
        {
            var result = default(string);

            var relevants = menuItems?
                .OfType<ItemViewModel>().ToArray();

            if (relevants?.Any() == true)
            {
                var first = relevants[0].Entry.Track.Info;
                var last = relevants[^1].Entry.Track.Info;

                var length = string.Concat(first.TakeWhile((c, i) => c == last[i])).Length + Constants.MenuGroupTextLength;

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

        private void ExecuteCallback()
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);

            executeCallback?.Invoke();

            Update();
        }

        private void UpdateItem()
        {
            IsPlayed = Entry.IsPlayed;
            IsSelected = Entry.IsSelected;
        }

        private void UpdateItems()
        {
            Selected = MenuItems?.OfType<ItemViewModel>()
                .FirstOrDefault(m => m.Entry?.IsSelected == true);

            IsSelected = Selected != default;
        }

        private void UpdateTrack()
        {
            var duration = Entry.Track?.Duration.ToString(Constants.FormatDuration);

            Header = $"{Entry.Track.Info} ({duration}) [{Entry.Track.Registers.Count}]";

            UpdateItem();
        }

        #endregion Private Methods
    }
}