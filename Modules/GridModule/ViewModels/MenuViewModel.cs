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

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class MenuViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly Action callback;
        private readonly Func<bool> isCheckedGetter;

        private string header;
        private bool isChecked;
        private bool isPlayed;
        private bool isSelected;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(string header, Action callback, Func<bool> isCheckedGetter = default,
            MaterialIconKind? icon = default)
        {
            this.callback = callback;
            this.isCheckedGetter = isCheckedGetter;

            Command = new DelegateCommand(ExecuteCallback);
            Header = header;
            Icon = icon;

            UpdateIsChecked();
        }

        public MenuViewModel(string header, IEnumerable<BindableBase> menuItems, MaterialIconKind? icon = default)
        {
            MenuItems = new ObservableCollection<BindableBase>(menuItems);
            Command = new DelegateCommand(ExecuteCallback);
            Header = header;
            Icon = icon;

            UpdateItems();
        }

        public MenuViewModel(IEventAggregator eventAggregator, IEnumerable<BindableBase> menuItems)
        {
            eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: _ => UpdateItems(),
                keepSubscriberReferenceAlive: true);

            MenuItems = new ObservableCollection<BindableBase>(menuItems);
            Command = new DelegateCommand(ExecuteCallback);
            Header = GetHeader();

            UpdateItems();
        }

        public MenuViewModel(IEventAggregator eventAggregator, Entry entry, Action callback)
        {
            this.callback = callback;

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

        public MenuViewModel Selected { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private void ExecuteCallback()
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);

            callback?.Invoke();

            UpdateIsChecked();
        }

        private string GetHeader()
        {
            var result = default(string);

            var relevants = MenuItems
                .OfType<MenuViewModel>().ToArray();

            if (relevants?.Any() == true)
            {
                var first = relevants.First().Entry.Track.Info;
                var last = relevants.Last().Entry.Track.Info;

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

        private void UpdateIsChecked()
        {
            if (isCheckedGetter != default)
            {
                IsChecked = isCheckedGetter.Invoke();
            }
        }

        private void UpdateItem()
        {
            IsPlayed = Entry.IsPlayed;
            IsSelected = Entry.IsSelected;
        }

        private void UpdateItems()
        {
            Selected = MenuItems?.OfType<MenuViewModel>()
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