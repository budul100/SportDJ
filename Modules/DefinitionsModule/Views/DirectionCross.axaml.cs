using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace SportDJ.Modules.DefinitionsModule.Views;

public partial class DirectionCross
    : UserControl
{
    #region Public Fields

    public static readonly StyledProperty<MaterialIconKind> CenterKindProperty = AvaloniaProperty.Register<DirectionCross, MaterialIconKind>(
        name: nameof(CenterKind),
        defaultValue: MaterialIconKind.Plus);

    public static readonly StyledProperty<ICommand> CommandProperty = AvaloniaProperty.Register<DirectionCross, ICommand>(
        name: nameof(Command));

    #endregion Public Fields

    #region Public Constructors

    public DirectionCross()
    {
        InitializeComponent();
    }

    #endregion Public Constructors

    #region Public Properties

    public MaterialIconKind CenterKind
    {
        get => GetValue(CenterKindProperty);

        set => SetValue(
            property: CenterKindProperty,
            value: value);
    }

    public ICommand Command
    {
        get => GetValue(CommandProperty);

        set => SetValue(
            property: CommandProperty,
            value: value);
    }

    #endregion Public Properties
}
