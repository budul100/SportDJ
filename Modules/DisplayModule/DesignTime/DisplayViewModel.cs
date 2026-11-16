using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace SportDJ.Modules.DisplayModule.DesignTime
{
    public class DisplayViewModel
        : ViewModels.DisplayViewModel
    {
        #region Public Constructors

        public DisplayViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default,
                  panelService: default, settingsService: default, sessionService: default)
        {
            Content = "Example - Example";
            Remaining = "00:05.3";

            Bitmap = LoadSample();
            Progress = 0.2;
        }

        #endregion Public Constructors

        #region Private Methods

        private static Bitmap LoadSample()
        {
            // Relative to the application directory instead of the working directory,
            // and without exception if the sample is missing
            var path = Path.Combine(AppContext.BaseDirectory, "Additionals", "Samples", "Sample1.png");

            return File.Exists(path)
                ? new Bitmap(path)
                : default;
        }

        #endregion Private Methods
    }
}
