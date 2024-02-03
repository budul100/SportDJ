using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Events.Output;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class PlayerViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly Player player;
        private readonly IStorageService<Set> settingsService;

        private IStorageFolder inputDirectory;
        private ObservableCollection<BindableBase> menus;
        private IOutputService outputServiceLive;
        private IPlayerService playerService;

        #endregion Private Fields

        #region Public Constructors

        public PlayerViewModel(IContainerProvider containerProvider, Player player, GridViewModel parent)
            : base(player)
        {
            this.containerProvider = containerProvider;
            this.player = player;

            settingsService = containerProvider.Resolve<IStorageService<Set>>();
            dialogService = containerProvider.Resolve<IDialogService>();

            ExecuteCommand = new DelegateCommand(
                executeMethod: ExecutePlayer);

            TrackBackwardCommand = new DelegateCommand(
                executeMethod: () => playerService?.Previous(),
                canExecuteMethod: () => IsAvailableSelection);

            TrackForwardCommand = new DelegateCommand(
                executeMethod: () => playerService?.Next(),
                canExecuteMethod: () => IsAvailableSelection);

            ResetCommand = new DelegateCommand(
                executeMethod: ResetStack,
                canExecuteMethod: () => IsAvailableReset);

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator?.GetEvent<PlayerDefinedEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<PlayerUpdatedEvent>().Subscribe(
                action: _ => UpdateDuration(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: p => playerService == p);

            eventAggregator?.GetEvent<StackDefinedEvent>().Subscribe(
                action: _ => UpdateMenu(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => playerService?.StackService == s);

            eventAggregator?.GetEvent<StackResetEvent>().Subscribe(
                action: _ => UpdateContent(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: s => playerService?.StackService == s);

            eventAggregator?.GetEvent<OutputUpdatedEvent>().Subscribe(
                action: _ => UpdateOutput(),
                keepSubscriberReferenceAlive: true);

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

            Intialize();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler MenuCloseEvent;

        public event EventHandler MenuOpenEvent;

        #endregion Public Events

        #region Public Properties

        public Bitmap Bitmap => playerService?.Track?.Bitmap;

        public string Content => playerService?.Track?.Info;

        public string Counter => playerService != default
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
            get { return player.IsForeground; }
            set
            {
                if (value != player.IsForeground)
                {
                    player.IsForeground = value;

                    settingsService.Save();

                    if (playerService != default)
                    {
                        playerService.MixerType = player.IsForeground
                            ? MixerType.Foreground
                            : MixerType.Background;
                    }

                    RaisePropertyChanged(nameof(IsForeground));
                }
            }
        }

        public bool IsLoop
        {
            get { return player.IsLoop; }
            set
            {
                if (value != player.IsLoop)
                {
                    player.IsLoop = value;

                    settingsService.Save();

                    if (playerService != default)
                    {
                        playerService.IsLoop = player.IsLoop;
                    }

                    RaisePropertyChanged(nameof(IsLoop));
                }
            }
        }

        public virtual bool IsPlaying => playerService?.IsPlaying == true;

        public string Key => KeyCode.GetKey();

        public string KeyCode
        {
            get { return player.KeyCode; }
            set
            {
                if (value != player.KeyCode)
                {
                    player.KeyCode = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(KeyCode));
                    RaisePropertyChanged(nameof(HasKey));
                }
            }
        }

        public ObservableCollection<BindableBase> Menus => settingsService.Contents.Options.EnableInterruptingDialogs || !outputServiceLive.IsPlaying
            ? menus
            : default;

        public string Name
        {
            get { return player.Name; }
            set
            {
                if (value != player.Name)
                {
                    player.Name = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public double? Progress => playerService?.Progress;

        public string Remaining => playerService.Remaining?.ToString(Constants.FormatDuration);

        public DelegateCommand ResetCommand { get; }

        public bool SavePosition
        {
            get { return player.SavePosition; }
            set
            {
                if (value != player.SavePosition)
                {
                    player.SavePosition = value;

                    settingsService.Save();

                    RaisePropertyChanged(nameof(SavePosition));
                }
            }
        }

        public DelegateCommand TrackBackwardCommand { get; }

        public DelegateCommand TrackForwardCommand { get; }

        #endregion Public Properties

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

        private IEnumerable<BindableBase> GetMenuItems()
        {
            var selectFileItem = new MenuViewModel(
                header: $"{Constants.SelectionFileTitle}...",
                callback: () => SetPlayerFileAsync());

            yield return selectFileItem;

            var selectFolderItem = new MenuViewModel(
                header: $"{Constants.SelectionFolderTitle}...",
                callback: () => SetPlayerFolderAsync());

            yield return selectFolderItem;

            if (player.Path != default)
            {
                var resetPlayerItem = new MenuViewModel(
                    header: Constants.ResetPlayerTitle,
                    callback: () => ResetPlayerAsync());

                yield return resetPlayerItem;

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
                            menuItems: group);

                        result.CloseMenuEvent += OnMenuClosing;

                        yield return result;
                    }
                }
            }
        }

        private IEnumerable<MenuViewModel> GetMenuTracks()
        {
            if (playerService?.StackService?.Entries?.Any() == true)
            {
                for (var index = 0; index < playerService.StackService.Entries.Count; index++)
                {
                    var current = index;

                    var result = new MenuViewModel(
                        containerProvider: containerProvider,
                        entry: playerService?.StackService.Entries[index],
                        callback: () => playerService?.StackService.Select(current));

                    result.CloseMenuEvent += OnMenuClosing;

                    yield return result;
                }
            }
        }

        private void Intialize()
        {
            playerService = containerProvider?.Resolve<IPlayerService>();

            var mixerType = IsForeground
                ? MixerType.Foreground
                : MixerType.Background;

            playerService.Initialize(
                path: player.Path,
                name: player.Name,
                savePosition: player.SavePosition,
                isLoop: player.IsLoop,
                mixerType: mixerType);

            UpdateOutput();
            UpdateMenu();
        }

        private void OnMenuClosing(object sender, EventArgs e)
        {
            MenuCloseEvent?.Invoke(
                sender: this,
                e: default);
        }

        private async void ResetPlayerAsync()
        {
            if (player.Path != default
                && !IsActive)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Constants.ResetPlayerText,
                    contentTitle: Constants.ResetPlayerTitle,
                    buttonDefinitions: ButtonEnum.YesNo,
                    enterDefaultButton: ClickEnum.No);

                if (result == ButtonResult.Yes)
                {
                    player.Path = default;
                    settingsService.Save();

                    Intialize();
                }
            }
        }

        private void ResetStack()
        {
            playerService?.Reset();
        }

        private async void SetPlayerFileAsync()
        {
            if (inputDirectory == default)
            {
                var folderPath = Path.GetDirectoryName(player.Path);
                inputDirectory = await dialogService.GetFolderAsync(folderPath);
            }

            var paths = await dialogService.OpenFilePickerAsync(
                title: Constants.SelectionFileTitle,
                allowMultiple: false,
                startLocation: inputDirectory);

            var path = paths.Any()
                ? paths.First().Path.AbsolutePath
                : default;

            if (File.Exists(path))
            {
                player.Path = path;
                settingsService.Save();

                Intialize();
            }
        }

        private async void SetPlayerFolderAsync()
        {
            if (inputDirectory == default)
            {
                inputDirectory = await dialogService.GetFolderAsync(player.Path);
            }

            var paths = await dialogService.OpenFolderPickerAsync(
                title: Constants.SelectionFileTitle,
                allowMultiple: false,
                startLocation: inputDirectory);

            var path = paths.Any()
                ? paths.First().Path.AbsolutePath
                : default;

            if (Directory.Exists(path))
            {
                player.Path = path;
                settingsService.Save();

                Intialize();
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

        private void UpdateMenu()
        {
            menus = new ObservableCollection<BindableBase>(GetMenuItems());
        }

        private void UpdateOutput()
        {
            outputServiceLive = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.Live));

            var outputServicePFL = containerProvider?.Resolve<IOutputService>(
                nameof(OutputType.PFL));

            HasPFL = outputServicePFL?.IsActive == true;

            UpdateContent();
        }

        #endregion Private Methods
    }
}