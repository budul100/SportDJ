using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;

namespace SportDJ.Shareds.UserControls
{
    public partial class WaveRenderer
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<Bitmap> BitmapProperty = AvaloniaProperty.Register<WaveRenderer, Bitmap>(
            name: nameof(Bitmap));

        public static readonly DirectProperty<WaveRenderer, bool> IsPlayingProperty = AvaloniaProperty.RegisterDirect<WaveRenderer, bool>(
            name: nameof(IsPlaying),
            getter: o => o.Progress > 0);

        public static readonly StyledProperty<double> OpacityPlayedProperty = AvaloniaProperty.Register<WaveRenderer, double>(
            name: nameof(Opacity),
            defaultValue: Defaults.TrackOpacityPlayed);

        public static readonly DirectProperty<WaveRenderer, double> PlayedProperty = AvaloniaProperty.RegisterDirect<WaveRenderer, double>(
            name: nameof(Played),
            getter: o => GetPlayed(o.Progress));

        public static readonly DirectProperty<WaveRenderer, double> ProgressProperty = AvaloniaProperty.RegisterDirect<WaveRenderer, double>(
            name: nameof(Progress),
            getter: o => o.Progress,
            setter: (o, v) => o.Progress = v);

        public static readonly DirectProperty<WaveRenderer, double> RemainingProperty = AvaloniaProperty.RegisterDirect<WaveRenderer, double>(
            name: nameof(Remaining),
            getter: o => GetRemaining(o.Progress));

        #endregion Public Fields

        #region Private Fields

        private bool isPlaying;
        private double played;
        private double progress;
        private double remaining;

        #endregion Private Fields

        #region Public Constructors

        public WaveRenderer()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Properties

        public Bitmap Bitmap
        {
            get => GetValue(BitmapProperty);

            set => SetValue(
                property: BitmapProperty,
                value: value);
        }

        public bool IsPlaying
        {
            get => Progress > 0;
        }

        public double OpacityPlayed
        {
            get => GetValue(OpacityPlayedProperty);

            set => SetValue(
                property: OpacityPlayedProperty,
                value: value);
        }

        public double Played
        {
            get => GetPlayed(Progress);
        }

        public double Progress
        {
            get => progress;

            set
            {
                if (value != progress)
                {
                    SetAndRaise(
                        property: ProgressProperty,
                        field: ref progress,
                        value: value);

                    SetAndRaise(
                        property: IsPlayingProperty,
                        field: ref isPlaying,
                        value: progress > 0);

                    SetAndRaise(
                        property: PlayedProperty,
                        field: ref played,
                        value: GetPlayed(progress));

                    SetAndRaise(
                        property: RemainingProperty,
                        field: ref remaining,
                        value: GetRemaining(progress));
                }
            }
        }

        public double Remaining
        {
            get => GetRemaining(Progress);
        }

        #endregion Public Properties

        #region Private Methods

        private static double GetPlayed(double progress)
        {
            return progress * Constants.WidthBase;
        }

        private static double GetRemaining(double progress)
        {
            return Constants.WidthBase - (progress * Constants.WidthBase);
        }

        #endregion Private Methods
    }
}