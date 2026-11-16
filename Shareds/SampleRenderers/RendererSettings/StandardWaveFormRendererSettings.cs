using SkiaSharp;

namespace NAudio.WaveFormRenderer.RendererSettings;

public class StandardWaveFormRendererSettings 
    : WaveFormRendererSettings
{
    #region Public Constructors

    public StandardWaveFormRendererSettings()
    {
        PixelsPerPeak = 1;
        SpacerPixels = 0;
        TopPeakShader = SKShader.CreateColor(SKColors.Maroon);
        BottomPeakShader = SKShader.CreateColor(SKColors.Peru);
    }

    #endregion Public Constructors

    #region Public Properties

    public override SKShader BottomPeakShader { get; set; }

    // not needed
    public override SKShader BottomSpacerShader { get; set; }

    public override SKShader TopPeakShader { get; set; }

    // not needed
    public override SKShader TopSpacerShader { get; set; }

    #endregion Public Properties
}