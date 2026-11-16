using SkiaSharp;

namespace NAudio.WaveFormRenderer.RendererSettings;

public class WaveFormRendererSettings
{
    #region Protected Constructors

    protected WaveFormRendererSettings()
    {
        Width = 800;
        TopHeight = 50;
        BottomHeight = 50;
        PixelsPerPeak = 1;
        SpacerPixels = 0;
        BackgroundColor = SKColors.Beige;
    }

    #endregion Protected Constructors

    #region Public Properties

    public SKColor BackgroundColor { get; set; }

    public SKBitmap BackgroundImage { get; set; }

    public int BottomHeight { get; set; }

    public virtual SKShader BottomPeakShader { get; set; }

    public virtual SKShader BottomSpacerShader { get; set; }

    public bool DecibelScale { get; set; }

    // for display purposes only
    public string Name { get; set; }

    public int PixelsPerPeak { get; set; }

    public int SpacerPixels { get; set; }

    public int TopHeight { get; set; }

    public virtual SKShader TopPeakShader { get; set; }

    public virtual SKShader TopSpacerShader { get; set; }

    public int Width { get; set; }

    #endregion Public Properties

    #region Protected Methods

    protected static SKShader CreateGradientShader(int height, SKColor startColor, SKColor endColor)
    {
        return SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, height),
            [startColor, endColor],
            [0, 1],
            SKShaderTileMode.Clamp);
    }

    #endregion Protected Methods
}