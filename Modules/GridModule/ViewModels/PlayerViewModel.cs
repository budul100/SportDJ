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
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IPlayerService playerService;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, Player player, GridViewModel parent)
            : base(containerProvider: containerProvider, element: player)
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
                action: _ => Initialize(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerUpdateEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerShiftedEvent>().Subscribe(
                action: _ => UpdateDuration(),
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
                action: OnKeyPressed,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: k => parent.IsVisible
                    && k == player.KeyCode);

            playerService?.Initialize(
                player: player);

            Initialize();
        }

        #endregion Public Constructors

        #region Public Properties

        public virtual Bitmap Bitmap => playerService?.Track?.Bitmap;

        public virtual string Content => playerService?.Track?.Info;

        public virtual string Counter => playerService?.Track?.Counter > 0
            ? $"({playerService.Track.Counter})"
            : default;

        public DelegateCommand ExecuteCommand { get; }

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

        public double? Progress => playerService?.Progress;

        public virtual string Remaining => playerService?.Remaining?.ToString(Constants.FormatDuration);

        public DelegateCommand ResetCommand { get; }

        public bool ResetPosition
        {
            get { return playerService?.ResetPosition == true; }
            set
            {
                if (playerService != default
                    && value != playerService.ResetPosition)
                {
                    playerService.ResetPosition = value;

                    RaisePropertyChanged(nameof(ResetPosition));
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

            base.Dispose(
                isDisposing: isDisposing);
        }

        protected override IEnumerable<BindableBase> GetElementMenus()
        {
            var gridMenus = GetGridMenus().ToArray();

            var gridMenu = new MenuViewModel(
                header: $"{Texts.GridEditHeader}...",
                menuItems: gridMenus,
                closeCallback: CloseMenu);

            yield return gridMenu;

            var playerMenus = GetPlayerMenus().ToArray();

            var playerMenu = new MenuViewModel(
                header: $"{Texts.PlayerEditHeader}...",
                menuItems: playerMenus,
                closeCallback: CloseMenu);

            yield return playerMenu;

            if (playerService?.HasPaths == true)
            {
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
                            menuItems: group,
                            closeCallback: CloseMenu);

                        yield return result;
                    }
                }
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            UpdateDuration();
            UpdateContent();
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
            if (IsRenaming)
            {
                IsRenaming = false;
            }
            else
            {
                if (playerService?.IsAvailable == true)
                {
                    playerService.Execute();
                }
                else
                {
                    OpenMenu();
                }
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
                        entry: entry,
                        closeCallback: CloseMenu);

                    yield return result;
                }
            }
        }

        private IEnumerable<BindableBase> GetPlayerMenus()
        {
            var selectFileItem = new MenuViewModel(
                header: $"{Texts.PlayerSelectionFileHeader}...",
                executeCallback: () => playerService?.SetFileAsync(),
                closeCallback: CloseMenu,
                icon: MaterialIconKind.FileMultipleOutline);

            yield return selectFileItem;

            var selectFolderItem = new MenuViewModel(
                header: $"{Texts.PlayerSelectionFolderHeader}...",
                executeCallback: () => playerService?.SetFolderAsync(),
                closeCallback: CloseMenu,
                icon: MaterialIconKind.FolderMultipleOutline);

            yield return selectFolderItem;

            var resetItem = new MenuViewModel(
                header: Texts.PlayerResetHeader,
                executeCallback: () => playerService.ClearAsync(),
                closeCallback: CloseMenu,
                icon: MaterialIconKind.CloseCircleOutline);

            yield return resetItem;

            yield return new SeparatorViewModel();

            var renameItem = new MenuViewModel(
                header: Texts.PlayerRenameHeader,
                executeCallback: RenamePlayer,
                closeCallback: CloseMenu,
                icon: MaterialIconKind.FormTextbox);

            yield return renameItem;

            var listenItem = new MenuViewModel(
                header: Texts.KeyListenHeader,
                executeCallback: ListenHook,
                isCheckedGetter: () => isListening,
                icon: MaterialIconKind.KeyboardOutline);

            listenChecker = listenItem.Update;

            yield return listenItem;

            var unhookItem = new MenuViewModel(
                header: Texts.KeyUnhookHeader,
                executeCallback: () => KeyCode = default,
                closeCallback: CloseMenu,
                icon: MaterialIconKind.KeyboardOffOutline);

            yield return unhookItem;

            if (playerService?.HasPaths == true)
            {
                yield return new SeparatorViewModel();

                var isForegroundItem = new MenuViewModel(
                    header: Texts.PlayerOnForegroundHeader,
                    executeCallback: () => IsForeground = !IsForeground,
                    closeCallback: CloseMenu,
                    isCheckedGetter: () => IsForeground,
                    icon: MaterialIconKind.ChevronUpBoxOutline);

                yield return isForegroundItem;

                var resetPositionsItem = new MenuViewModel(
                    header: Texts.PlayerSavePosition,
                    executeCallback: () => ResetPosition = !ResetPosition,
                    closeCallback: CloseMenu,
                    isCheckedGetter: () => ResetPosition,
                    icon: MaterialIconKind.FileRestoreOutline);

                yield return resetPositionsItem;

                if (playerService.HasStack)
                {
                    yield return new SeparatorViewModel();

                    var shuffleItem = new MenuViewModel(
                        header: Texts.PlayerShuffleHeader,
                        executeCallback: () => playerService.ShuffleAsync(),
                        closeCallback: CloseMenu,
                        icon: MaterialIconKind.ShuffleVariant);

                    yield return shuffleItem;
                }
            }
        }

        private void OnKeyPressed(string keyCode)
        {
            if (isListening)
            {
                ReceiveKey(keyCode);
            }
            else
            {
                ExecutePlayer();
            }
        }

        private void RenamePlayer()
        {
            if (playerService?.IsActive != true)
            {
                IsRenaming = true;
            }
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