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

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
        {
            this.containerProvider = containerProvider;

            settingsService.Load(Defaults.FileNameSettings);
            sessionService.Load(Defaults.FileNameSession);

            var set = settingsService.Contents;

            XXXX(set);
            settingsService.Save();

            InitializeOutput(
                type: OutputType.Live);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Background,
                onForeground: false);

            InitializeMixer(
                outputType: OutputType.Live,
                mixerType: MixerType.Foreground,
                onForeground: true);

            InitializeOutput(
                type: OutputType.PFL);

            InitializeMixer(
                outputType: OutputType.PFL,
                mixerType: MixerType.PFL,
                onForeground: true);
        }

        #endregion Public Constructors

        #region Private Methods

        private void InitializeMixer(OutputType outputType, MixerType mixerType, bool onForeground)
        {
            var outputService = containerProvider.Resolve<IOutputService>(
                name: outputType.ToString());

            if (outputService?.Format != default)
            {
                var mixerService = containerProvider.Resolve<IMixerService>(
                    name: mixerType.ToString());

                mixerService.Initialize(
                    type: mixerType,
                    waveFormat: outputService.Format,
                    playsMultipleSignals: onForeground);

                outputService.Add(
                    mixerService: mixerService,
                    onForeground: onForeground);
            }
        }

        private void InitializeOutput(OutputType type)
        {
            var outputService = containerProvider.Resolve<IOutputService>(
                name: type.ToString());

            outputService.Initialize(
                type: type);
        }

        private void XXXX(Set set)
        {
            set.Grid.Cells.Clear();

            set.Grid.Cells.Add(new Player() { Name = "Player 1", Row = 0, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample1.mp3" });
            set.Grid.Cells.Add(new Player() { Name = "Player 2", Row = 0, Column = 1, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample2.mp3" });
            set.Grid.Cells.Add(new Player() { Name = "Player 3", Row = 1, Column = 0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples", SavePositions = true });

            var grid1 = new Grid
            {
                Name = "Group 1",
            };

            grid1.Cells.Add(new Player() { Name = "Player 11", Row = 0, Column = 0 });
            grid1.Cells.Add(new Player() { Name = "Player 12", Row = 1, Column = 0 });
            grid1.Cells.Add(new Player() { Name = "Player 13", Row = 0, Column = 1 });
            grid1.Cells.Add(new Player() { Name = "Player 14", Row = 1, Column = 1 });

            var grid2 = new Grid
            {
                Name = "Group 2",
            };

            grid2.Cells.Add(new Player() { Name = "Player 21", Row = 0, Column = 0 });
            grid2.Cells.Add(new Player() { Name = "Player 22", Row = 0, Column = 1 });
            grid2.Cells.Add(new Player() { Name = "Player 23", Row = 0, Column = 2 });
            grid2.Cells.Add(new Player() { Name = "Player 24", Row = 1, Column = 1 });

            var group = new Group
            {
                Row = 0,
                Column = 2,
            };

            group.Grids.Add(grid1);
            group.Grids.Add(grid2);

            set.Grid.Cells.Add(group);

            set.Options.FadeOnLowerMilliseconds = 50;
            set.Options.FadeOnPauseMilliseconds = 500;
            set.Options.FadeOnMixInMilliseconds = 500;
            set.Options.FadeOnMixOutMilliseconds = 500;
            set.Options.SelectNextSeconds = 5;

            set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Lautsprecher (Realtek(R) Audio)";
            //set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Kopfhoerer (Realtek(R) Audio)";
        }

        #endregion Private Methods
    }
}