using Avalonia.Media;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Extensions;

public static class PlayerColorExtensions
{
    #region Private Fields

    // Share of the hue in the muted result
    private const double MixShare = 0.38;

    // Same as ColorBackground in Ressources.axaml
    private static readonly Color baseColor = Color.FromRgb(0x33, 0x33, 0x33);

    #endregion Private Fields

    #region Public Methods

    public static IBrush GetBrush(this PlayerColorType colorType)
    {
        var hue = GetHue(colorType);

        return new SolidColorBrush(
            color: Color.FromRgb(
                r: Mix(baseColor.R, hue.R),
                g: Mix(baseColor.G, hue.G),
                b: Mix(baseColor.B, hue.B)));
    }

    #endregion Public Methods

    #region Private Methods

    private static Color GetHue(PlayerColorType colorType)
    {
        return colorType switch
        {
            PlayerColorType.Red => Color.FromRgb(0xE5, 0x39, 0x35),
            PlayerColorType.Orange => Color.FromRgb(0xFB, 0x8C, 0x00),
            PlayerColorType.Yellow => Color.FromRgb(0xFD, 0xD8, 0x35),
            PlayerColorType.Green => Color.FromRgb(0x43, 0xA0, 0x47),
            PlayerColorType.Teal => Color.FromRgb(0x00, 0xAC, 0xC1),
            PlayerColorType.Blue => Color.FromRgb(0x1E, 0x88, 0xE5),
            PlayerColorType.Pink => Color.FromRgb(0xD8, 0x1B, 0x60),
            PlayerColorType.Gray => Color.FromRgb(0x9E, 0x9E, 0x9E),
            PlayerColorType.Purple => Color.FromRgb(0x8E, 0x24, 0xAA),
            PlayerColorType.Lime => Color.FromRgb(0xC0, 0xCA, 0x33),
            PlayerColorType.Brown => Color.FromRgb(0x8D, 0x6E, 0x63),
            PlayerColorType.Cyan => Color.FromRgb(0x29, 0xB6, 0xF6),
            PlayerColorType.Indigo => Color.FromRgb(0x39, 0x49, 0xAB),
            PlayerColorType.Coral => Color.FromRgb(0xFF, 0x70, 0x43),
            PlayerColorType.Mint => Color.FromRgb(0x69, 0xF0, 0xAE),
            PlayerColorType.White => Color.FromRgb(0xEC, 0xEF, 0xF1),
            _ => baseColor,
        };
    }

    private static byte Mix(byte baseValue, byte hueValue)
    {
        return (byte)(baseValue + ((hueValue - baseValue) * MixShare));
    }

    #endregion Private Methods
}
