using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
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

        public MainViewModel(IContainerProvider containerProvider, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
        {
            this.containerProvider = containerProvider;

            settingsService.Load(Defaults.FileNameSettings);
            sessionService.Load(Defaults.FileNameSession);

            var set = settingsService.Contents;

            InitializeOutput();
            InitializePlayersLive();

            XXXX(set);

            settingsService.Save();
        }

        #endregion Public Constructors

        #region Private Methods

        private static void XXXX(Set set)
        {
            set.Grid.Contents.Clear();

            set.Grid.Contents.Add(new Player() { Name = "Player 1", Row = 0, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample1.mp3" });

            set.Grid.Contents.Add(new Player() { Name = "Player 2", Row = 0, Column = 1, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample2.mp3" });

            set.Grid.Contents.Add(new Player() { Name = "Player 3", Row = 1, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples", SavePositions = true });

            var group1 = new Group
            {
                Name = "Group 1",
                Row = 0,
                Column = 0,
                Rows = 2,
                Columns = 2,
            };

            group1.Contents.Add(new Player() { Name = "Player 11", Row = 0, Column = 0 });

            group1.Contents.Add(new Player() { Name = "Player 12", Row = 1, Column = 0 });

            group1.Contents.Add(new Player() { Name = "Player 13", Row = 0, Column = 1 });

            group1.Contents.Add(new Player() { Name = "Player 14", Row = 1, Column = 1 });

            var group2 = new Group
            {
                Name = "Group 2",
                Row = 0,
                Column = 0,
                Rows = 2,
                Columns = 2,
            };

            group2.Contents.Add(new Player() { Name = "Player 21", Row = 0, Column = 0 });

            group2.Contents.Add(new Player() { Name = "Player 22", Row = 1, Column = 0 });

            group2.Contents.Add(new Player() { Name = "Player 23", Row = 0, Column = 1 });

            group2.Contents.Add(new Player() { Name = "Player 24", Row = 1, Column = 1 });

            var group3 = new Group
            {
                Name = "Group 12",
                Row = 0,
                Column = 2,
                Rows = 2,
                Columns = 2,
            };

            group3.Contents.Add(group1);
            group3.Contents.Add(group2);

            set.Grid.Contents.Add(group3);

            set.Grid.Rows = 2;
            set.Grid.Columns = 4;

            set.Options.FadeOnLowerMilliseconds = 50;
            set.Options.FadeOnPauseMilliseconds = 500;
            set.Options.FadeOnMixInMilliseconds = 500;
            set.Options.FadeOnMixOutMilliseconds = 500;
            set.Options.SelectNextSeconds = 5;

            set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Lautsprecher (Realtek(R) Audio)";
            //set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Kopfhoerer (Realtek(R) Audio)";
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
                var playerBackground = containerProvider.Resolve<IPlayerService>(
                    name: nameof(PlayerType.Background));

                playerBackground.Initialize(
                    type: PlayerType.Background,
                    waveFormat: outputLive.Format,
                    isMultiSignalPlayer: false);

                outputLive.Add(
                    playerService: playerBackground,
                    isForegroundPlayer: false);

                var playerForeground = containerProvider.Resolve<IPlayerService>(
                    name: nameof(PlayerType.Foreground));

                playerForeground.Initialize(
                    type: PlayerType.Foreground,
                    waveFormat: outputLive.Format,
                    isMultiSignalPlayer: true);

                outputLive.Add(
                    playerService: playerForeground,
                    isForegroundPlayer: true);
            }
        }

        #endregion Private Methods
    }
}