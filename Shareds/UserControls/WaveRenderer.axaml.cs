using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SportDJ.Commons.Assets;

namespace SportDJ.Shareds.UserControls;

/// <summary>
/// Draws a wave bitmap. While playing, the part left of the progress position is drawn with reduced opacity.
/// Rendered directly instead of using three images with clip geometries, so progress updates do not allocate.
/// </summary>
public class WaveRenderer
    : Control
{
    #region Fields

    public static readonly StyledProperty<Bitmap> BitmapProperty = AvaloniaProperty
        .Register<WaveRenderer, Bitmap>(
            name: nameof(Bitmap));

    public static readonly DirectProperty<WaveRenderer, bool> IsPlayingProperty = AvaloniaProperty
        .RegisterDirect<WaveRenderer, bool>(
            name: nameof(IsPlaying),
            getter: o => o.isPlaying);

    public static readonly StyledProperty<double> OpacityPlayedProperty = AvaloniaProperty
        .Register<WaveRenderer, double>(
            name: nameof(OpacityPlayed),
            defaultValue: Defaults.TrackOpacityPlayed);

    public static readonly StyledProperty<double> ProgressProperty = AvaloniaProperty
        .Register<WaveRenderer, double>(
            name: nameof(Progress));
    private bool isPlaying;

    #endregion Fields

    #region Private Constructors

    static WaveRenderer()
    {
        AffectsMeasure<WaveRenderer>(BitmapProperty);

        AffectsRender<WaveRenderer>(
            BitmapProperty,
            OpacityPlayedProperty,
            ProgressProperty);
    }

    #endregion Private Constructors

    #region Public Properties

    public Bitmap Bitmap
    {
        get => GetValue(BitmapProperty);

        set => SetValue(
            property: BitmapProperty,
            value: value);
    }

    public bool IsPlaying => isPlaying;

    public double OpacityPlayed
    {
        get => GetValue(OpacityPlayedProperty);

        set => SetValue(
            property: OpacityPlayedProperty,
            value: value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);

        set => SetValue(
            property: ProgressProperty,
            value: value);
    }

    #endregion Public Properties

    #region Public Methods

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bitmap = Bitmap;

        if (bitmap == default
            || Bounds.Width <= 0
            || Bounds.Height <= 0)
        {
            return;
        }

        var source = new Rect(bitmap.Size);
        var destination = new Rect(Bounds.Size);
        var progress = GetProgress(Progress);

        if (progress <= 0)
        {
            context.DrawImage(bitmap, source, destination);

            return;
        }

        var splitX = destination.Width * progress;

        var playedClip = new Rect(
            x: 0,
            y: 0,
            width: splitX,
            height: destination.Height);

        var remainingClip = new Rect(
            x: splitX,
            y: 0,
            width: destination.Width - splitX,
            height: destination.Height);

        if (playedClip.Width > 0)
        {
            using (context.PushClip(playedClip))
            using (context.PushOpacity(OpacityPlayed))
            {
                context.DrawImage(bitmap, source, destination);
            }
        }

        if (remainingClip.Width > 0)
        {
            using (context.PushClip(remainingClip))
            {
                context.DrawImage(bitmap, source, destination);
            }
        }
    }

    #endregion Public Methods

    #region Protected Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Bitmap?.Size ?? default;

        // Same behavior as an image with Stretch.Fill: use the available space, fall back to the natural size
        return new Size(
            width: double.IsInfinity(availableSize.Width) ? size.Width : availableSize.Width,
            height: double.IsInfinity(availableSize.Height) ? size.Height : availableSize.Height);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ProgressProperty)
        {
            SetAndRaise(
                property: IsPlayingProperty,
                field: ref isPlaying,
                value: GetProgress(Progress) > 0);
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static double GetProgress(double progress)
    {
        return double.IsNaN(progress)
            ? 0
            : Math.Clamp(
                value: progress,
                min: 0,
                max: 1);
    }

    #endregion Private Methods
}
