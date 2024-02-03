using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SportDJ.Commons.Assets;
using System;

namespace SportDJ.Commons.Controls
{
    public partial class FlashingPanel
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<IBrush> ColorProperty = AvaloniaProperty.Register<FlashingPanel, IBrush>(
            name: nameof(Color));

        public static readonly DirectProperty<FlashingPanel, double> DurationMilliSecsProperty = AvaloniaProperty.RegisterDirect<FlashingPanel, double>(
            name: nameof(DurationMilliSecs),
            getter: o => o.DurationMilliSecs,
            setter: (o, v) => o.DurationMilliSecs = v);

        public static readonly DirectProperty<FlashingPanel, TimeSpan> DurationProperty = AvaloniaProperty.RegisterDirect<FlashingPanel, TimeSpan>(
            name: "Duration",
            getter: o => GetDuration(o.DurationMilliSecs));

        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<FlashingPanel, bool>(
            name: nameof(IsActive));

        public static readonly StyledProperty<double> OpacityMaxProperty = AvaloniaProperty.Register<FlashingPanel, double>(
            name: nameof(OpacityMax),
            defaultValue: Defaults.FlashingOpacityMax);

        public static readonly StyledProperty<double> OpacityMinProperty = AvaloniaProperty.Register<FlashingPanel, double>(
            name: nameof(OpacityMin),
            defaultValue: Defaults.FlashingOpacityMin);

        #endregion Public Fields

        #region Private Fields

        private TimeSpan duration;

        private double durationMilliSecs;

        #endregion Private Fields

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

        public double DurationMilliSecs
        {
            get => durationMilliSecs;

            set
            {
                if (value != durationMilliSecs)
                {
                    SetAndRaise(
                        property: DurationMilliSecsProperty,
                        field: ref durationMilliSecs,
                        value: value);

                    SetAndRaise(
                        property: DurationProperty,
                        field: ref duration,
                        value: GetDuration(durationMilliSecs));
                }
            }
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

        #region Private Methods

        private static TimeSpan GetDuration(double durationMilliSecs)
        {
            return TimeSpan.FromMilliseconds(durationMilliSecs);
        }

        #endregion Private Methods
    }
}