using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.DefinitionsModule.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        #region Private Fields

        private const string Titel = "Settings";

        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Set> settingsService;

        private Player player;

        #endregion Private Fields

        #region Public Constructors

        public SettingsViewModel(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            RegisterCommand = new DelegateCommand(RegisterPlayer);
            UnregisterCommand = new DelegateCommand(UnegisterPlayer);
        }

        #endregion Public Constructors

        #region Public Properties

        public static int MaxLengthName => Constants.NameMaxLength;

        public static string Title => Titel;

        public bool IsForeground
        {
            get => player?.IsForeground == true;

            set
            {
                if (player?.PlayerService != default
                    && value != player.PlayerService.IsForeground)
                {
                    player.PlayerService.IsForeground = value;

                    settingsService.Save();

                    eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);

                    RaisePropertyChanged(nameof(IsForeground));
                }
            }
        }

        public bool IsReset
        {
            get => player?.ResetPosition == true;

            set
            {
                if (player?.PlayerService != default
                    && value != player.PlayerService.ResetPosition)
                {
                    player.PlayerService.ResetPosition = value;

                    settingsService.Save();

                    eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);

                    RaisePropertyChanged(nameof(IsReset));
                }
            }
        }

        public string Name
        {
            get => player?.Name;
            set
            {
                if (player != default
                    && value != player.Name)
                {
                    player.Name = value;

                    settingsService.Save();

                    eventAggregator.GetEvent<PlayerUpdateEvent>().Publish(player);

                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public DelegateCommand RegisterCommand { get; private set; }

        public DelegateCommand UnregisterCommand { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Set(Player player)
        {
            if (player != default
                && this.player != player)
            {
                this.player = player;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void RegisterPlayer()
        {
            if (player?.PlayerService != default)
            {
                _ = player.PlayerService.RegisterAsync();
            }
        }

        private void UnegisterPlayer()
        {
            player?.PlayerService?.Unregister();
        }

        #endregion Private Methods
    }
}