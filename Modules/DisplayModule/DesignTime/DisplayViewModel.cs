using Avalonia.Media.Imaging;

namespace SportDJ.Modules.DisplayModule.DesignTime
{
    public class DisplayViewModel
        : ViewModels.DisplayViewModel
    {
        #region Public Constructors

        public DisplayViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default,
                  settingsService: default, sessionService: default)
        {
            Content = "Example - Example";
            Remaining = "00:05.3";
            IsEnding = true;

            Bitmap = new Bitmap(@"C:\Users\mgr\Entwicklung\SportDJ\Additionals\Samples\Sample1.png");

            Progress = 0.2;
        }

        #endregion Public Constructors
    }
}