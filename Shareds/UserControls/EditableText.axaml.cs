using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;

namespace SportDJ.Shareds.UserControls
{
    public partial class EditableText
        : UserControl
    {
        #region Public Fields

        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<EditableText, bool>(
            name: nameof(IsActive));

        public static readonly StyledProperty<int> MaxLengthProperty = AvaloniaProperty.Register<EditableText, int>(
            name: nameof(MaxLength));

        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<EditableText, string>(
            name: nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

        #endregion Public Fields

        #region Private Fields

        // Text at the moment the editing started, used to revert on Escape
        private string textBeforeEdit;

        #endregion Private Fields

        #region Public Constructors

        public EditableText()
        {
            InitializeComponent();
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

        #region Protected Methods

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsActiveProperty
                && change.GetNewValue<bool>())
            {
                textBeforeEdit = Text;

                // The TextBox becomes visible with this change, so focus it afterwards
                Dispatcher.Post(
                    action: () => InputBox.Focus(),
                    priority: DispatcherPriority.Input);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnGotFocus(object sender, FocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Dispatcher.Post(
                    action: textBox.SelectAll,
                    priority: DispatcherPriority.Input);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox)
            {
                return;
            }

            if (e.Key == Key.Enter)
            {
                IsActive = false;

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Text = textBeforeEdit;

                IsActive = false;

                e.Handled = true;
            }
        }

        private void OnLostFocus(object sender, FocusChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                IsActive = false;
            }
        }

        #endregion Private Methods
    }
}
