using Avalonia.Media;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Extensions;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class ColorOptionViewModel(PlayerColorType colorType)
    : BindableBase
{
    #region Private Fields

    private bool isSelected;

    #endregion Private Fields

    #region Public Properties

    public IBrush Brush { get; } = colorType.GetBrush();

    public PlayerColorType ColorType { get; } = colorType;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public string Name => ColorType.ToString();

    #endregion Public Properties
}
