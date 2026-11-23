using Avalonia.Controls;
using Avalonia.Input;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Hook;

namespace SportDJ.App.Views;

public partial class MainView
    : Window
{
    #region Private Fields

    private readonly IEventAggregator eventAggregator;

    #endregion Private Fields

    #region Public Constructors

    public MainView()
        : this(default)
    { }

    public MainView(IEventAggregator eventAggregator)
    {
        this.eventAggregator = eventAggregator;

        this.KeyDown += OnKeyDown;
        this.KeyUp += OnKeyUp;

        InitializeComponent();
    }

    #endregion Public Constructors

    #region Private Methods

    private bool HasTarget()
    {
        // Only controls which consume typed keys block the hotkeys, otherwise a focused
        // button or check box would silently disable them until the next click elsewhere
        var focusedElement = FocusManager.GetFocusedElement();

        return focusedElement is TextBox or ComboBox;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!HasTarget())
        {
            var keyCode = e?.PhysicalKey.ToString();

            if (!string.IsNullOrWhiteSpace(keyCode))
            {
                eventAggregator?.GetEvent<KeyPressedEvent>()?.Publish(keyCode);
            }
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (!HasTarget())
        {
            var keyCode = e?.PhysicalKey.ToString();

            if (!string.IsNullOrWhiteSpace(keyCode))
            {
                eventAggregator?.GetEvent<KeyReleasedEvent>()?.Publish(keyCode);
            }
        }
    }

    #endregion Private Methods
}
