using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;

namespace SportDJ.Shareds.UserControls
{
    public partial class WaveRenderer
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<Bitmap> BitmapProperty = AvaloniaProperty
            .Register<WaveRenderer, Bitmap>(
                name: nameof(Bitmap));

        public static readonly DirectProperty<WaveRenderer, bool> IsPlayingProperty = AvaloniaProperty
            .RegisterDirect<WaveRenderer, bool>(
                name: nameof(IsPlaying),
                getter: o => o.Progress > 0);

        public static readonly StyledProperty<double> OpacityPlayedProperty = AvaloniaProperty
            .Register<WaveRenderer, double>(
                name: nameof(OpacityPlayed),
                defaultValue: Defaults.TrackOpacityPlayed);

        public static readonly DirectProperty<WaveRenderer, Geometry> PlayedClipProperty = AvaloniaProperty
            .RegisterDirect<WaveRenderer, Geometry>(
                name: nameof(PlayedClip),
                getter: o => GetPlayedClip(o.Progress, (int)o.Bounds.Width, (int)o.Bounds.Height));

        public static readonly DirectProperty<WaveRenderer, double> ProgressProperty = AvaloniaProperty
            .RegisterDirect<WaveRenderer, double>(
                name: nameof(Progress),
                getter: o => o.Progress,
                setter: (o, v) => o.Progress = v);

        public static readonly DirectProperty<WaveRenderer, RectangleGeometry> RemainingClipProperty = AvaloniaProperty
            .RegisterDirect<WaveRenderer, RectangleGeometry>(
                name: nameof(RemainingClip),
                getter: o => GetRemainingClip(o.Progress, o.Bounds.Width, o.Bounds.Height));

        #endregion Public Fields

        #region Private Fields

        private double progress;

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

        public Geometry PlayedClip => GetPlayedClip(
            progress: Progress,
            totalWidth: Bounds.Width,
            totalHeight: Bounds.Height);

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

                    RaisePropertyChanged(
                        property: IsPlayingProperty,
                        oldValue: default,
                        newValue: IsPlaying);
                }
            }
        }

        public RectangleGeometry RemainingClip => GetRemainingClip(
            progress: Progress,
            totalWidth: Bounds.Width,
            totalHeight: Bounds.Height);

        #endregion Public Properties

        #region Protected Methods

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ProgressProperty)
            {
                RaisePropertyChanged(
                    property: IsPlayingProperty,
                    oldValue: default,
                    newValue: IsPlaying);
            }

            if (change.Property == ProgressProperty || change.Property == BoundsProperty)
            {
                RaisePropertyChanged(
                    property: PlayedClipProperty,
                    oldValue: default,
                    newValue: PlayedClip);

                RaisePropertyChanged(
                    property: RemainingClipProperty,
                    oldValue: default,
                    newValue: RemainingClip);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static RectangleGeometry GetPlayedClip(double progress, double totalWidth, double totalHeight)
        {
            var clipWidth = totalWidth * progress;

            if (totalWidth <= 0 || clipWidth <= 0)
                return default;

            var rect = new Rect(
                x: 0,
                y: 0,
                width: clipWidth,
                height: totalHeight);

            return new RectangleGeometry(rect);
        }

        private static RectangleGeometry GetRemainingClip(double progress, double totalWidth, double totalHeight)
        {
            var startX = totalWidth * progress;

            var clipWidth = totalWidth - startX;

            if (totalWidth <= 0 || clipWidth <= 0)
                return default;

            var rect = new Rect(
                x: startX,
                y: 0,
                width: clipWidth,
                height: totalHeight);

            return new RectangleGeometry(rect);
        }

        #endregion Private Methods
    }
}