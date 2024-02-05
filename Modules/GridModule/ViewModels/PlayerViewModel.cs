using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Material.Icons;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IPlayerService playerService;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, Player player, GridViewModel parent)
            : base(containerProvider: containerProvider, cell: player)
        {
            playerService = containerProvider?.Resolve<IPlayerService>();

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecutePlayer);

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => playerService?.Previous(),
                canExecuteMethod: () => IsAvailableSelection);

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => playerService?.Next(),
                canExecuteMethod: () => IsAvailableSelection);

            ResetCommand = new DelegateCommand(
                executeMethod: () => playerService?.Reset(),
                canExecuteMethod: () => IsAvailableReset);

            eventAggregator?.GetEvent<PlayerInitializedEvent>().Subscribe(
                action: _ => base.Initialize(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerUpdatedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerShiftedEvent>().Subscribe(
                action: _ => UpdateDuration(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerDefinedEvent>().Subscribe(
                action: _ => UpdateMenu(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<OutputStartedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputServiceLive == o);

            eventAggregator?.GetEvent<OutputStoppedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: o => outputServiceLive == o);

            eventAggregator?.GetEvent<KeyPressedEvent>().Subscribe(
                action: _ => ExecutePlayer(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: k => parent.IsVisible
                    && k == player.KeyCode);

            playerService.Initialize(
                player: player);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        public event EventHandler MenuOpenEvent;

        #endregion Public Events

        #region Public Properties

        public Bitmap Bitmap => playerService?.Track?.Bitmap;

        public string Content => playerService?.Track?.Info;

        public string Counter => playerService?.Track != default
            ? $"({playerService.Counter})"
            : default;

        public DelegateCommand ExecuteCommand { get; }

        public bool HasKey => !string.IsNullOrWhiteSpace(Key);

        public bool HasPFL { get; private set; }

        public bool HasPFLOrKey => HasPFL || HasKey;

        public bool HasStack => playerService?.HasStack == true;

        public virtual bool IsActive => playerService?.IsActive == true;

        public bool IsAvailableExecute => playerService?.IsAvailable == true;

        public bool IsAvailableReset => playerService?.HasPlayeds == true;

        public bool IsAvailableSelection => playerService?.HasUnplayeds == true;

        public bool IsEnding => playerService?.IsEnding == true;

        public bool IsForeground
        {
            get { return playerService?.IsForeground == true; }
            set
            {
                if (playerService != default
                    && value != playerService.IsForeground)
                {
                    playerService.IsForeground = value;

                    RaisePropertyChanged(nameof(IsForeground));
                }
            }
        }

        public bool IsLoop
        {
            get { return playerService?.IsLoop == true; }
            set
            {
                if (playerService != default
                    && value != playerService.IsLoop)
                {
                    playerService.IsLoop = value;

                    RaisePropertyChanged(nameof(IsLoop));
                }
            }
        }

        public virtual bool IsPlaying => playerService?.IsPlaying == true;

        public string Key => KeyCode.GetKey();

        public string KeyCode
        {
            get { return playerService?.KeyCode; }
            set
            {
                if (playerService != default
                    && value != playerService.KeyCode)
                {
                    playerService.KeyCode = value;

                    RaisePropertyChanged(nameof(KeyCode));
                    RaisePropertyChanged(nameof(HasKey));
                }
            }
        }

        public string Name
        {
            get { return playerService.Name; }
            set
            {
                if (playerService != default
                    && value != playerService.Name)
                {
                    playerService.Name = value;

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public double? Progress => playerService?.Progress;

        public string Remaining => playerService.Remaining?.ToString(Constants.FormatDuration);

        public DelegateCommand ResetCommand { get; }

        public bool SavePosition
        {
            get { return playerService.SavePosition; }
            set
            {
                if (playerService != default
                    && value != playerService.SavePosition)
                {
                    playerService.SavePosition = value;

                    RaisePropertyChanged(nameof(SavePosition));
                }
            }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

        #endregion Public Properties

        #region Protected Methods

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    playerService?.Dispose();
                }

                isDisposed = true;
            }
        }

        protected override IEnumerable<BindableBase> GetMenuItems()
        {
            var menuCells = GetMenuCells().ToArray();

            var editCellsItem = new MenuViewModel(
                header: $"{Constants.GridEditHeader}...",
                menuItems: menuCells);

            editCellsItem.MenuCloseEvent += OnMenuClosing;

            yield return editCellsItem;

            yield return new SeparatorViewModel();

            var selectFileItem = new MenuViewModel(
                header: $"{Constants.SelectionFileHeader}...",
                callback: () => playerService?.SetFileAsync(),
                icon: MaterialIconKind.FileMultipleOutline);

            yield return selectFileItem;

            var selectFolderItem = new MenuViewModel(
                header: $"{Constants.SelectionFolderHeader}...",
                callback: () => playerService?.SetFolderAsync(),
                icon: MaterialIconKind.FolderMultipleOutline);

            yield return selectFolderItem;

            if (playerService?.HasPaths == true)
            {
                var clearPlayerItem = new MenuViewModel(
                    header: Constants.PlayerClearHeader,
                    callback: () => playerService.ClearAsync(),
                    icon: MaterialIconKind.CloseCircleOutline);

                yield return clearPlayerItem;

                yield return new SeparatorViewModel();

                var isForegroundItem = new MenuViewModel(
                    header: Constants.PlayerOnForegroundHeader,
                    callback: () => IsForeground = !IsForeground,
                    isCheckedGetter: () => IsForeground,
                    icon: MaterialIconKind.ChevronUpBox);

                yield return isForegroundItem;

                var menuItems = GetMenuTracks()
                    .OrderBy(m => m.Header).ToArray();

                if (menuItems?.Length > 1)
                {
                    yield return new SeparatorViewModel();

                    var groupsSize = (menuItems.Length / ((menuItems.Length / Constants.MenuGroupBaseSize) + 1)) + 1;

                    var groups = menuItems
                        .Chunk(groupsSize).ToArray();

                    foreach (var group in groups)
                    {
                        var result = new MenuViewModel(
                            eventAggregator: eventAggregator,
                            menuItems: group);

                        result.MenuCloseEvent += OnMenuClosing;

                        yield return result;
                    }
                }
            }
        }

        protected override void UpdateOutput()
        {
            base.UpdateOutput();

            var outputServicePFL = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.PFL));

            HasPFL = outputServicePFL?.IsActive == true;

            UpdateContent();
        }

        #endregion Protected Methods

        #region Private Methods

        private void ExecutePlayer()
        {
            if (playerService?.IsAvailable == true)
            {
                playerService.Execute();
            }
            else
            {
                MenuOpenEvent?.Invoke(
                    sender: this,
                    e: default);
            }
        }

        private IEnumerable<MenuViewModel> GetMenuTracks()
        {
            if (playerService?.Entries?.Any() == true)
            {
                foreach (var entry in playerService.Entries)
                {
                    var result = new MenuViewModel(
                        eventAggregator: eventAggregator,
                        entry: entry);

                    result.MenuCloseEvent += OnMenuClosing;

                    yield return result;
                }
            }
        }

        private void OnMenuClosing(object sender, EventArgs e)
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void UpdateContent()
        {
            RaisePropertyChanged(nameof(Counter));
            RaisePropertyChanged(nameof(Content));
            RaisePropertyChanged(nameof(Bitmap));

            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(IsPlaying));
            RaisePropertyChanged(nameof(IsEnding));

            RaisePropertyChanged(nameof(IsAvailableExecute));
            RaisePropertyChanged(nameof(IsAvailableSelection));
            RaisePropertyChanged(nameof(IsAvailableReset));
            RaisePropertyChanged(nameof(IsAvailableMenu));

            RaisePropertyChanged(nameof(HasKey));
            RaisePropertyChanged(nameof(HasPFL));
            RaisePropertyChanged(nameof(HasPFLOrKey));
            RaisePropertyChanged(nameof(HasStack));

            RaisePropertyChanged(nameof(Menus));

            ExecuteCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();

            TrackBackwardCommand.RaiseCanExecuteChanged();
            TrackForwardCommand.RaiseCanExecuteChanged();
        }

        private void UpdateDuration()
        {
            RaisePropertyChanged(nameof(Progress));
            RaisePropertyChanged(nameof(Remaining));
        }

        #endregion Private Methods
    }
}