using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Dialogs;

namespace SportDJ.App
{
    public static class Program
    {
        #region Public Methods

        public static AppBuilder BuildAvaloniaApp() => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                EnableMultiTouch = true,
                UseDBusMenu = true
            })
            .With(new Win32PlatformOptions())
            .UseSkia()
            .UseManagedSystemDialogs();

        #endregion Public Methods

        #region Private Methods

        private static int Main(string[] args)
        {
            var builder = BuildAvaloniaApp();

            // InitializeLogging();

            if (args.Contains("--fbdev"))
            {
                SilenceConsole();
                return builder.StartLinuxFbDev(args, scaling: GetScaling(args));
            }
            else if (args.Contains("--drm"))
            {
                SilenceConsole();
                return builder.StartLinuxDrm(args, scaling: GetScaling(args));
            }
            else
            {
                return builder.StartWithClassicDesktopLifetime(args);
            }
        }

        private static double GetScaling(string[] args)
        {
            var idx = Array.IndexOf(args, "--scaling");

            if (idx >= 0
                && args.Length > idx + 1
                && double.TryParse(args[idx + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var scaling))
            {
                return scaling;
            }

            return 1;
        }

        private static void SilenceConsole()
        {
            new Thread(() =>
            {
                Console.CursorVisible = false;
                while (true)
                    Console.ReadKey(true);
            })
            { IsBackground = true }.Start();
        }

        #endregion Private Methods
    }
}
