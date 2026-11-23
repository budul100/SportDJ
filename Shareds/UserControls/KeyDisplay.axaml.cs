using Avalonia;
using Avalonia.Controls;
using SportDJ.Commons.Extensions;

namespace SportDJ.Shareds.UserControls;

public partial class KeyDisplay
    : UserControl
{
    #region Public Fields

    public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<KeyDisplay, string>(
        name: nameof(Description));

    public static readonly StyledProperty<bool> IsNumpadProperty = AvaloniaProperty.Register<KeyDisplay, bool>(
        name: nameof(IsNumpad));

    public static readonly StyledProperty<string> KeyCodeProperty = AvaloniaProperty.Register<KeyDisplay, string>(
        name: nameof(KeyCode));

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<KeyDisplay, string>(
        name: nameof(Text));

    #endregion Public Fields

    #region Private Fields

    private const string DescriptionNumpad = "Numeric keypad";

    #endregion Private Fields

    #region Public Constructors

    public KeyDisplay()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Tooltip of the badge, empty for keys of the main block.
    /// </summary>
    public string Description
    {
        get => GetValue(DescriptionProperty);

        private set => SetValue(
            property: DescriptionProperty,
            value: value);
    }

    public bool IsNumpad
    {
        get => GetValue(IsNumpadProperty);

        private set => SetValue(
            property: IsNumpadProperty,
            value: value);
    }

    /// <summary>
    /// Key code as it is stored (e.g. "NumPad1" or "Enter").
    /// </summary>
    public string KeyCode
    {
        get => GetValue(KeyCodeProperty);

        set => SetValue(
            property: KeyCodeProperty,
            value: value);
    }

    /// <summary>
    /// Text shown for the key code, without any decoration.
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);

        private set => SetValue(
            property: TextProperty,
            value: value);
    }

    #endregion Public Properties

    #region Protected Methods

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == KeyCodeProperty)
        {
            var keyCode = KeyCode;

            Text = keyCode.GetKey();
            IsNumpad = keyCode.IsNumpad();
            Description = IsNumpad
                ? DescriptionNumpad
                : default;
        }
    }

    #endregion Protected Methods
}
