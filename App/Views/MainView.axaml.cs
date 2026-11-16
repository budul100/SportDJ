using Avalonia.Controls;
using Avalonia.Input;
using Prism.Events;
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
        var focusedElement = FocusManager.GetFocusedElement();

        return focusedElement != default
            && focusedElement?.GetType() != typeof(TabItem);
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