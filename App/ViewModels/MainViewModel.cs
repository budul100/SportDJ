using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System.Linq;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private IOutputService outputLive;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, ISettingsService<Session> settingsService)
        {
            this.containerProvider = containerProvider;

            var session = settingsService.Settings;

            InitializeOutput();
            InitializePlayersLive();

            XXXX(session, containerProvider.Resolve<IOutputService>(name: OutputType.Live.ToString()).GetDevices().ElementAt(3).Value);

            settingsService.Save();
        }

        #endregion Public Constructors

        #region Private Methods

        private static void XXXX(Session session, string device)
        {
            session.Set.Contents.Clear();

            session.Set.Contents.Add(new Controller() { Name = "Player 1", Row = 0, Column = 0 });

            session.Set.Contents.Add(new Controller() { Name = "Player 2", Row = 1, Column = 0 });

            session.Set.Contents.Add(new Controller() { Name = "Player 3", Row = 0, Column = 1 });

            var group = new Group
            {
                Name = "Group1",
                Row = 0,
                Column = 2,
                Rows = 2,
                Columns = 2,
            };

            group.Contents.Add(new Controller() { Name = "Player 21", Row = 0, Column = 0 });

            group.Contents.Add(new Controller() { Name = "Player 22", Row = 1, Column = 0 });

            group.Contents.Add(new Controller() { Name = "Player 23", Row = 0, Column = 1 });

            group.Contents.Add(new Controller() { Name = "Player 24", Row = 1, Column = 1 });

            session.Set.Contents.Add(group);

            session.Set.Rows = 2;
            session.Set.Columns = 4;

            session.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = device;
        }

        private void InitializeOutput()
        {
            outputLive = containerProvider.Resolve<IOutputService>(
                name: nameof(OutputType.Live));

            outputLive.Initialize(
                type: OutputType.Live);
        }

        private void InitializePlayersLive()
        {
            if (outputLive?.Format != default)
            {
                var playerMusic = containerProvider.Resolve<IPlayerService>(
                    name: nameof(PlayerType.Music));

                playerMusic.Initialize(
                    type: PlayerType.Music,
                    waveFormat: outputLive.Format,
                    isMultiSignalPlayer: false);

                outputLive.Add(
                    playerService: playerMusic,
                    isForegroundPlayer: false);

                var playerEffects = containerProvider.Resolve<IPlayerService>(
                    name: nameof(PlayerType.Effects));

                playerEffects.Initialize(
                    type: PlayerType.Effects,
                    waveFormat: outputLive.Format,
                    isMultiSignalPlayer: true);

                outputLive.Add(
                    playerService: playerEffects,
                    isForegroundPlayer: false);
            }
        }

        #endregion Private Methods
    }
}