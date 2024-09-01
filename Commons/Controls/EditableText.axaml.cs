using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SportDJ.Commons.Controls
{
    public partial class EditableText
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<EditableText, bool>(
            name: nameof(IsActive),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<EditableText, int>(
            name: nameof(MaxLength));

        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<EditableText, string>(
            name: nameof(Text),
            defaultBindingMode: BindingMode.TwoWay);

        #endregion Public Fields

        #region Public Constructors

        public EditableText()
        {
            InitializeComponent();

            var textBox = this
                .FindControl<TextBox>("TextBox");

            textBox?.AddHandler(
                routedEvent: KeyDownEvent,
                handler: OnKeyDown,
                routes: RoutingStrategies.Tunnel);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);

            set => SetValue(
                property: IsActiveProperty,
                value: value);
        }

        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);

            set => SetValue(
                property: MaxLengthProperty,
                value: value);
        }

        public string Text
        {
            get => GetValue(TextProperty);

            set => SetValue(
                property: TextProperty,
                value: value);
        }

        #endregion Public Properties

        #region Private Methods

        private void OnGotFocus(object sender, GotFocusEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            // Defer the SelectAll() method call using the dispatcher

            Dispatcher.UIThread.Post(textBox.SelectAll);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox))
            {
                return;
            }

            if (e.Key == Key.Return
                || e.Key == Key.Enter
                || e.Key == Key.Escape)
            {
                IsActive = false;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox))
            {
                return;
            }

            IsActive = false;
        }

        #endregion Private Methods
    }
}