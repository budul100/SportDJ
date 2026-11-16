using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace SportDJ.Modules.ControllerModule.Behaviors;

public class PointerCaptureBehavior
    : Behavior<InputElement>
{
    #region Public Fields

    public static readonly StyledProperty<TimeSpan> HoldDurationProperty =
        AvaloniaProperty.Register<PointerCaptureBehavior, TimeSpan>(
            name: nameof(HoldDuration),
            defaultValue: TimeSpan.FromMilliseconds(HoldDurationDefault));

    public static readonly StyledProperty<ICommand> HoldEndCommandProperty =
        AvaloniaProperty.Register<PointerCaptureBehavior, ICommand>(
            name: nameof(HoldEndCommand));

    public static readonly StyledProperty<ICommand> HoldStartCommandProperty =
        AvaloniaProperty.Register<PointerCaptureBehavior, ICommand>(
            name: nameof(HoldStartCommand));

    public static readonly AttachedProperty<bool> IsPointerPressedProperty =
        AvaloniaProperty.RegisterAttached<PointerCaptureBehavior, InputElement, bool>(
            name: "IsPointerPressed",
            defaultValue: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand> TapCommandProperty =
        AvaloniaProperty.Register<PointerCaptureBehavior, ICommand>(
            name: nameof(TapCommand));

    #endregion Public Fields

    #region Private Fields

    private const int HoldDurationDefault = 400;

    private DispatcherTimer holdTimer;
    private bool isHolding;
    private bool wasPressed;

    #endregion Private Fields

    #region Public Properties

    public TimeSpan HoldDuration
    {
        get => GetValue(HoldDurationProperty);
        set => SetValue(HoldDurationProperty, value);
    }

    public ICommand HoldEndCommand
    {
        get => GetValue(HoldEndCommandProperty);
        set => SetValue(HoldEndCommandProperty, value);
    }

    public ICommand HoldStartCommand
    {
        get => GetValue(HoldStartCommandProperty);
        set => SetValue(HoldStartCommandProperty, value);
    }

    public ICommand TapCommand
    {
        get => GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    #endregion Public Properties

    #region Public Methods

    public static bool GetIsPointerPressed(AvaloniaObject obj) => obj.GetValue(IsPointerPressedProperty);

    public static void SetIsPointerPressed(AvaloniaObject obj, bool value) => obj.SetValue(IsPointerPressedProperty, value);

    #endregion Public Methods

    #region Protected Methods

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.AddHandler(
                routedEvent: InputElement.PointerPressedEvent,
                handler: OnPointerPressed,
                routes: RoutingStrategies.Bubble,
                handledEventsToo: true);

            AssociatedObject.AddHandler(
                routedEvent: InputElement.PointerReleasedEvent,
                handler: OnPointerReleased,
                routes: RoutingStrategies.Bubble,
                handledEventsToo: true);
        }
    }

    protected override void OnDetaching()
    {
        StopHoldTimer();

        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(
                routedEvent: InputElement.PointerPressedEvent,
                handler: OnPointerPressed);

            AssociatedObject.RemoveHandler(
                routedEvent: InputElement.PointerReleasedEvent,
                handler: OnPointerReleased);
        }

        base.OnDetaching();
    }

    #endregion Protected Methods

    #region Private Methods

    private void OnHoldTimerElapsed(object sender, EventArgs e)
    {
        StopHoldTimer();

        isHolding = true;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject == null) return;

        wasPressed = GetIsPointerPressed(this);

        SetIsPointerPressed(
            obj: this,
            value: true);

        if (!wasPressed)
        {
            if (HoldStartCommand?.CanExecute(null) == true)
            {
                HoldStartCommand.Execute(null);
            }
        }

        e.Pointer.Capture(AssociatedObject);

        StartHoldTimer();

        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (AssociatedObject == null || e.Pointer.Captured != AssociatedObject) return;

        StopHoldTimer();

        if (isHolding || wasPressed)
        {
            if (HoldEndCommand?.CanExecute(null) == true)
            {
                HoldEndCommand.Execute(null);
            }

            SetIsPointerPressed(
                obj: this,
                value: false);
        }
        else
        {
            if (TapCommand?.CanExecute(null) == true)
            {
                TapCommand.Execute(null);
            }

            SetIsPointerPressed(
                obj: this,
                value: true);
        }

        e.Pointer.Capture(null);

        isHolding = false;
        e.Handled = true;
    }

    private void StartHoldTimer()
    {
        StopHoldTimer();

        holdTimer = new DispatcherTimer(
            interval: HoldDuration,
            priority: DispatcherPriority.Normal,
            callback: OnHoldTimerElapsed);

        isHolding = false;
        holdTimer.Start();
    }

    private void StopHoldTimer()
    {
        holdTimer?.Stop();
        holdTimer = null;
    }

    #endregion Private Methods
}