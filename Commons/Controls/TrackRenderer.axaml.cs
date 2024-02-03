using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Controls
{
    public partial class TrackRenderer
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<Bitmap> BitmapProperty = AvaloniaProperty.Register<TrackRenderer, Bitmap>(
            name: nameof(Bitmap));

        public static readonly DirectProperty<TrackRenderer, bool> IsPlayingProperty = AvaloniaProperty.RegisterDirect<TrackRenderer, bool>(
            name: "IsPlaying",
            getter: o => o.Progress > 0);

        public static readonly StyledProperty<double> OpacityPlayedProperty = AvaloniaProperty.Register<TrackRenderer, double>(
            name: nameof(Opacity),
            defaultValue: Defaults.TrackOpacityPlayed);

        public static readonly DirectProperty<TrackRenderer, double> PlayedProperty = AvaloniaProperty.RegisterDirect<TrackRenderer, double>(
            name: "Played",
            getter: o => GetPlayed(o.Progress));

        public static readonly DirectProperty<TrackRenderer, double> ProgressProperty = AvaloniaProperty.RegisterDirect<TrackRenderer, double>(
            name: nameof(Progress),
            getter: o => o.Progress,
            setter: (o, v) => o.Progress = v);

        public static readonly DirectProperty<TrackRenderer, double> RemainingProperty = AvaloniaProperty.RegisterDirect<TrackRenderer, double>(
            name: "Remaining",
            getter: o => GetRemaining(o.Progress));

        #endregion Public Fields

        #region Private Fields

        private bool isPlaying;
        private double played;
        private double progress;
        private double remaining;

        #endregion Private Fields

        #region Public Constructors

        public TrackRenderer()
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

        public double OpacityPlayed
        {
            get => GetValue(OpacityPlayedProperty);

            set => SetValue(
                property: OpacityPlayedProperty,
                value: value);
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