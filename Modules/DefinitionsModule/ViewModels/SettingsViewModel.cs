using Prism.Events;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Player;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.DefinitionsModule.ViewModels
{
    public class SettingsViewModel(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        : BindableBase
    {
        #region Private Fields

        private const string Titel = "Settings";
        private Player player;

        #endregion Private Fields

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
    }
}