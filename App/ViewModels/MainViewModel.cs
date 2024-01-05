using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
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
            set.Root.Contents.Clear();

            set.Root.Contents.Add(new Controller() { Name = "Player 1", Row = 0, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample1.mp3" });

            set.Root.Contents.Add(new Controller() { Name = "Player 2", Row = 0, Column = 1, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample2.mp3" });

            set.Root.Contents.Add(new Controller() { Name = "Player 3", Row = 1, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples", SavePositions = true });

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

            set.Root.Contents.Add(group);

            set.Root.Rows = 2;
            set.Root.Columns = 4;

            set.Options.FadeOnBackgroundMilliseconds = 50;
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