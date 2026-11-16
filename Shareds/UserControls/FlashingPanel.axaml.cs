using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using SportDJ.Commons.Assets;

namespace SportDJ.Shareds.UserControls;

public partial class FlashingPanel
    : UserControl
{
    #region Fields

    public static readonly StyledProperty<IBrush> ColorProperty = AvaloniaProperty.Register<FlashingPanel, IBrush>(
        name: nameof(Color));

    public static readonly StyledProperty<double> DurationMilliSecsProperty = AvaloniaProperty.Register<FlashingPanel, double>(
        name: nameof(DurationMilliSecs),
        defaultValue: DefaultDurationMilliSecs);

    public static readonly DirectProperty<FlashingPanel, TimeSpan> DurationProperty = AvaloniaProperty.RegisterDirect<FlashingPanel, TimeSpan>(
        name: nameof(Duration),
        getter: o => o.duration);

    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<FlashingPanel, bool>(
        name: nameof(IsActive));

    public static readonly StyledProperty<double> OpacityMaxProperty = AvaloniaProperty.Register<FlashingPanel, double>(
        name: nameof(OpacityMax),
        defaultValue: Defaults.FlashingOpacityMax);

    public static readonly StyledProperty<double> OpacityMinProperty = AvaloniaProperty.Register<FlashingPanel, double>(
        name: nameof(OpacityMin),
        defaultValue: Defaults.FlashingOpacityMin);
    // Used when no valid duration is set, an endless animation with zero duration must be avoided
    private const double DefaultDurationMilliSecs = 1000;

    private const string FlashingClass = "flashing";

    private Style animationStyle;
    private TimeSpan duration = TimeSpan.FromMilliseconds(DefaultDurationMilliSecs);
    private bool isAttached;

    #endregion Fields

    #region Public Constructors

    public FlashingPanel()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Public Properties

    public IBrush Color
    {
        get => GetValue(ColorProperty);

        set => SetValue(
            property: ColorProperty,
            value: value);
    }

    public TimeSpan Duration => duration;

    public double DurationMilliSecs
    {
        get => GetValue(DurationMilliSecsProperty);

        set => SetValue(
            property: DurationMilliSecsProperty,
            value: value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);

        set => SetValue(
            property: IsActiveProperty,
            value: value);
    }

    public double OpacityMax
    {
        get => GetValue(OpacityMaxProperty);

        set => SetValue(
            property: OpacityMaxProperty,
            value: value);
    }

    public double OpacityMin
    {
        get => GetValue(OpacityMinProperty);

        set => SetValue(
            property: OpacityMinProperty,
            value: value);
    }

    #endregion Public Properties

    #region Protected Methods

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        isAttached = true;

        RestartAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        isAttached = false;

        StopAnimation();

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DurationMilliSecsProperty)
        {
            SetAndRaise(
                property: DurationProperty,
                field: ref duration,
                value: GetDuration(change.GetNewValue<double>()));
        }

        if (change.Property == DurationMilliSecsProperty
            || change.Property == IsActiveProperty
            || change.Property == OpacityMaxProperty
            || change.Property == OpacityMinProperty)
        {
            RestartAnimation();
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static Animation CreateAnimation(TimeSpan duration, double opacityMin, double opacityMax)
    {
        var animation = new Animation
        {
            Duration = duration,
            IterationCount = IterationCount.Infinite,
        };

        animation.Children.Add(CreateKeyFrame(
            cue: 0.0,
            opacity: opacityMin));

        animation.Children.Add(CreateKeyFrame(
            cue: 0.5,
            opacity: opacityMax));

        animation.Children.Add(CreateKeyFrame(
            cue: 1.0,
            opacity: opacityMin));

        return animation;
    }

    private static KeyFrame CreateKeyFrame(double cue, double opacity)
    {
        var keyFrame = new KeyFrame
        {
            Cue = new Cue(cue),
        };

        keyFrame.Setters.Add(new Setter(
            property: OpacityProperty,
            value: opacity));

        return keyFrame;
    }

    private static TimeSpan GetDuration(double durationMilliSecs)
    {
        return TimeSpan.FromMilliseconds(durationMilliSecs > 0
            ? durationMilliSecs
            : DefaultDurationMilliSecs);
    }

    // Looping animations must not be started with Animation.RunAsync (it throws for infinite iterations).
    // They are attached as a style that is active while the panel has the flashing class.
    private void RestartAnimation()
    {
        StopAnimation();

        if (!isAttached || !IsActive || FlashPanel == default)
        {
            return;
        }

        var animation = CreateAnimation(
            duration: duration,
            opacityMin: OpacityMin,
            opacityMax: OpacityMax);

        animationStyle = new Style(x => x.OfType<Panel>().Class(FlashingClass));
        animationStyle.Animations.Add(animation);

        FlashPanel.Styles.Add(animationStyle);
        FlashPanel.Classes.Add(FlashingClass);
    }

    private void StopAnimation()
    {
        if (FlashPanel == default)
        {
            return;
        }

        FlashPanel.Classes.Remove(FlashingClass);

        if (animationStyle != default)
        {
            FlashPanel.Styles.Remove(animationStyle);

            animationStyle = default;
        }
    }

    #endregion Private Methods
}
