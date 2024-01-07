using Prism.Ioc;
using Prism.Mvvm;
using SharpHook;
using SharpHook.Native;
using SportDJ.Commons;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using SportDJ.Commons.Models.Structure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SportDJ.App.ViewModels
{
    public class MainViewModel
        : BindableBase, IDisposable
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private TaskPoolGlobalHook hook;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public MainViewModel(IContainerProvider containerProvider, IStorageService<Set> settingsService,
            IStorageService<Session> sessionService, TaskPoolGlobalHook hook)
        {
            //XXXX(settingsService?.Contents);
            //settingsService?.Save();

            this.containerProvider = containerProvider;
            this.hook = hook;

            LoadSessionService(
                settingsService: settingsService,
                sessionService: sessionService);

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

            if (hook != default)
            {
                Task.Run(() => hook.Run());
            }
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    hook.Dispose();
                    hook = default;
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void LoadSessionService(IStorageService<Set> settingsService,
            IStorageService<Session> sessionService)
        {
            var sessionPath = settingsService?.GetPath(
                appName: Constants.AppName,
                fileName: Defaults.FileNameSession);

            sessionService?.Load(sessionPath);
        }

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
            var outputService = containerProvider?.Resolve<IOutputService>(
                name: type.ToString());

            outputService?.Initialize(
                type: type);
        }

        private void XXXX(Set set)
        {
            if (set != default)
            {
                set.Grid.Cells.Clear();

                set.Grid.Cells.Add(new Player() { Name = "Player 1", Row = 0, Column = 0, Key = KeyCode.Vc0, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample1.mp3" });
                set.Grid.Cells.Add(new Player() { Name = "Player 2", Row = 0, Column = 1, Key = KeyCode.Vc1, Path = @"..\..\..\..\Tests\StackServiceTests\Samples\Sample2.mp3" });
                set.Grid.Cells.Add(new Player() { Name = "Player 3", Row = 1, Column = 0, Key = KeyCode.Vc2, Path = @"..\..\..\..\Tests\StackServiceTests\Samples", SavePositions = true });

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

                set.Options.KeyLowerVolume = KeyCode.VcSpace;
                set.Options.KeyPauseOutput = KeyCode.VcEnter;

                set.Options.FadeOnLowerMilliseconds = 50;
                set.Options.FadeOnPauseMilliseconds = 500;
                set.Options.FadeOnMixInMilliseconds = 500;
                set.Options.FadeOnMixOutMilliseconds = 500;

                set.Options.SelectNextSeconds = 5;

                set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Lautsprecher (Realtek(R) Audio)";
                //set.Outputs.Single(o => o.Type == OutputType.Live).DeviceName = "Kopfhoerer (Realtek(R) Audio)";
            }
        }

        #endregion Private Methods
    }
}