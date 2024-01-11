using Avalonia.Media.Imaging;

namespace SportDJ.Modules.DisplayModule.DesignTime
{
    public class DisplayViewModel
        : ViewModels.DisplayViewModel
    {
        #region Public Constructors

        public DisplayViewModel()
            : base(regionManager: default, containerProvider: default, eventAggregator: default,
                  sessionService: default)
        {
            Content = "Example - Example";
            Remaining = "00:05.3";
            IsEnding = true;

            Bitmap = new Bitmap(@"C:\Users\mgr\Entwicklung\SportDJ\Additionals\Samples\Sample1.png");
            WidthPosition = 20;
            WidthRemaining = 80;
        }

        #endregion Public Constructors
    }
}